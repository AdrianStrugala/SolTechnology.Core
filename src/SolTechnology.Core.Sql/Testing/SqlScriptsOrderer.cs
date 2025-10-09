using System.Text.RegularExpressions;

namespace SolTechnology.Core.Sql.Testing;

internal static class SqlScriptOrderer
{
    // Matches: CREATE TABLE [dbo].[City] ( ... )  OR  CREATE TABLE dbo.City ( ... )
    private static readonly Regex CreateTableRx = new(
        @"(?is)\bCREATE\s+TABLE\s+" +
        @"(?:(?<schema>```math
[^```]+```|[A-Za-z_][\w]*)\.)?" +
        @"(?<table>```math
[^```]+```|[A-Za-z_][\w]*)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches: REFERENCES [dbo].[City] (...) OR REFERENCES dbo.City (...)
    private static readonly Regex ReferencesRx = new(
        @"(?is)\bREFERENCES\s+" +
        @"(?:(?<schema>```math
[^```]+```|[A-Za-z_][\w]*)\.)?" +
        @"(?<table>```math
[^```]+```|[A-Za-z_][\w]*)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<string> OrderByDependencies(IEnumerable<string> files, Action<string>? log = null)
    {
        var list = files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        var createdByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // file -> created tables
        var creates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // table -> file
        var refsByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // file -> referenced tables
        var textCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in list)
        {
            var text = File.ReadAllText(f);
            textCache[f] = text;

            foreach (Match m in CreateTableRx.Matches(text))
            {
                var key = NormalizeName(GroupValue(m, "schema") ?? "dbo", GroupValue(m, "table"));
                if (!creates.ContainsKey(key))
                    creates[key] = f;

                if (!createdByFile.TryGetValue(f, out var set))
                    createdByFile[f] = set = new(StringComparer.OrdinalIgnoreCase);
                set.Add(key);
            }

            foreach (Match m in ReferencesRx.Matches(text))
            {
                var key = NormalizeName(GroupValue(m, "schema") ?? "dbo", GroupValue(m, "table"));
                if (!refsByFile.TryGetValue(f, out var set))
                    refsByFile[f] = set = new(StringComparer.OrdinalIgnoreCase);
                set.Add(key);
            }
        }

        log?.Invoke("Detected CREATE TABLE:");
        foreach (var kv in creates.GroupBy(kv => kv.Value))
            log?.Invoke($" - {kv.Key}: {string.Join(", ", kv.Select(e => e.Key))}");

        // Build graph: edge defFile -> refFile
        var adj = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var inDeg = list.ToDictionary(f => f, _ => 0, StringComparer.OrdinalIgnoreCase);

        foreach (var f in list)
        {
            if (!refsByFile.TryGetValue(f, out var refs)) continue;

            // Files this 'f' depends on
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in refs)
            {
                if (creates.TryGetValue(r, out var defFile) && !string.Equals(defFile, f, StringComparison.OrdinalIgnoreCase))
                {
                    deps.Add(defFile);
                }
            }

            foreach (var defFile in deps)
            {
                if (!adj.TryGetValue(defFile, out var outs))
                    adj[defFile] = outs = new(StringComparer.OrdinalIgnoreCase);
                if (outs.Add(f))
                    inDeg[f]++;
            }
        }

        var q = new Queue<string>(inDeg.Where(kv => kv.Value == 0).Select(kv => kv.Key).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        var ordered = new List<string>(list.Count);

        while (q.Count > 0)
        {
            var n = q.Dequeue();
            ordered.Add(n);

            if (!adj.TryGetValue(n, out var outs)) continue;
            foreach (var m in outs)
            {
                if (--inDeg[m] == 0)
                    q.Enqueue(m);
            }
        }

        // Any leftovers (cycles or unresolved deps) – append in stable order
        var leftovers = list.Except(ordered, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        if (leftovers.Count > 0)
        {
            log?.Invoke("Warning: unresolved or cyclic dependencies, appending at the end:");
            foreach (var lf in leftovers)
                log?.Invoke(" - " + lf);

            ordered.AddRange(leftovers);
        }

        log?.Invoke("Final script order:");
        foreach (var f in ordered)
            log?.Invoke(" - " + f);

        return ordered;
    }

    private static string? GroupValue(Match m, string name) => m.Groups[name].Success ? m.Groups[name].Value : null;

    private static string NormalizeName(string schema, string table)
    {
        static string Unwrap(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '[' && s[^1] == ']') return s[1..^1];
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') return s[1..^1];
            return s;
        }
        var sch = string.IsNullOrWhiteSpace(schema) ? "dbo" : Unwrap(schema);
        var tbl = Unwrap(table);
        return $"{sch}.{tbl}";
    }
}