using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Tests;

/// <summary>
/// Architecture fitness guard: asserts that <c>TreatWarningsAsErrors</c> is enforced repo-wide
/// and that only an explicit, commented allow-list suppresses it. Fails loudly on any new unlisted
/// suppression — designed to be edited (add to the allow-list with a reason), not bypassed.
/// </summary>
public sealed class BuildHygieneGuardTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>
    /// Projects that legitimately suppress <c>TreatWarningsAsErrors</c> at the project level.
    /// Each entry requires a documented reason — the guard ensures no NEW unlisted project
    /// silently disables warnings-as-errors.
    /// </summary>
    private static readonly HashSet<string> AllowedLaggards = new(StringComparer.OrdinalIgnoreCase)
    {
        // Deprecated (see docs/Cron.md) and removed from the solution (.slnx) — intentionally
        // frozen, not worth fixing warnings in dead code. The physical folder still lives under
        // src/ so the filesystem walk still sees it.
        "SolTechnology.Core.Scheduler",
    };

    /// <summary>
    /// The only <c>WarningsNotAsErrors</c> codes allowed in <c>src/Directory.Build.props</c>.
    /// Any new code must be added here with a comment explaining why.
    /// </summary>
    private static readonly HashSet<string> AllowedSuppressedWarnings = new(StringComparer.OrdinalIgnoreCase)
    {
        "NU1900", // NuGet Audit feed connectivity — not a CVE finding
        "NU1510", // "PackageReference will not be pruned" — shared-framework echo
    };

    [Test]
    public void Src_DirectoryBuildProps_Enables_TreatWarningsAsErrors()
    {
        var propsPath = Path.Combine(RepoRoot, "src", "Directory.Build.props");
        File.Exists(propsPath).Should().BeTrue($"expected {propsPath} to exist");

        var doc = XDocument.Load(propsPath);
        var twe = doc.Descendants("TreatWarningsAsErrors").FirstOrDefault();

        twe.Should().NotBeNull("src/Directory.Build.props must set TreatWarningsAsErrors");
        twe!.Value.Should().Be("true");
    }

    [Test]
    public void Src_DirectoryBuildProps_Only_Suppresses_AllowListed_Warnings()
    {
        var propsPath = Path.Combine(RepoRoot, "src", "Directory.Build.props");
        var doc = XDocument.Load(propsPath);
        var wna = doc.Descendants("WarningsNotAsErrors").FirstOrDefault();

        if (wna is null) return; // no suppressions — fine

        var codes = wna.Value
            .Replace("$(WarningsNotAsErrors)", "", StringComparison.OrdinalIgnoreCase)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var code in codes)
        {
            AllowedSuppressedWarnings.Should().Contain(code,
                $"Warning code '{code}' is suppressed in src/Directory.Build.props but not in the allow-list. " +
                "Add it to AllowedSuppressedWarnings with a reason comment, or remove the suppression.");
        }
    }

    [Test]
    public void No_Unlisted_Project_Disables_TreatWarningsAsErrors()
    {
        var srcDir = Path.Combine(RepoRoot, "src");
        var csprojFiles = Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories);

        var unlisted = new List<string>();

        foreach (var csproj in csprojFiles)
        {
            var doc = XDocument.Load(csproj);
            var twe = doc.Descendants("TreatWarningsAsErrors")
                .FirstOrDefault(e => e.Value.Equals("false", StringComparison.OrdinalIgnoreCase));

            if (twe is null) continue;

            var projectName = Path.GetFileNameWithoutExtension(csproj);
            if (!AllowedLaggards.Contains(projectName))
            {
                unlisted.Add(projectName);
            }
        }

        unlisted.Should().BeEmpty(
            "The following projects disable TreatWarningsAsErrors without being in the allow-list: " +
            $"[{string.Join(", ", unlisted)}]. Either fix the warnings or add to AllowedLaggards with a reason.");
    }

    [Test]
    public void No_Data_Messaging_Module_Has_AspNet_FrameworkReference()
    {
        // Blocker-1 fitness guard (ADR-012): data/messaging modules must NOT carry ASP.NET surface.
        var nonAspNetModules = new[]
        {
            "SolTechnology.Core.SQL",
            "SolTechnology.Core.Cache",
            "SolTechnology.Core.MessageBus",
        };

        var srcDir = Path.Combine(RepoRoot, "src");
        var violations = new List<string>();

        foreach (var module in nonAspNetModules)
        {
            var csproj = Path.Combine(srcDir, module, $"{module}.csproj");
            if (!File.Exists(csproj)) continue;

            var doc = XDocument.Load(csproj);
            var hasAspNet = doc.Descendants("FrameworkReference")
                .Any(e => e.Attribute("Include")?.Value
                    ?.Contains("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase) == true);

            if (hasAspNet)
            {
                violations.Add(module);
            }
        }

        violations.Should().BeEmpty(
            "Data/messaging modules must NOT reference Microsoft.AspNetCore.App. " +
            $"Violations: [{string.Join(", ", violations)}].");
    }

    private static string FindRepoRoot()
    {
        var dir = TestContext.CurrentContext.TestDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "SolTechnology.Core.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? throw new InvalidOperationException("Could not find repo root (SolTechnology.Core.slnx)");
    }
}

