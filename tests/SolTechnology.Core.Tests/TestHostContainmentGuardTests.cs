using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Tests;

/// <summary>
/// Architecture fitness guard: asserts that <c>WebApplicationFactory</c> / <c>APIFixture</c>
/// instantiation is contained to approved base classes — preventing ad-hoc test-host proliferation.
/// </summary>
public sealed class TestHostContainmentGuardTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>
    /// Files allowed to instantiate <c>WebApplicationFactory</c> or <c>APIFixture</c>.
    /// Everything else must go through these approved fixtures.
    /// </summary>
    private static readonly HashSet<string> ApprovedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Core testing library — the base fixture itself
        "APIFixture.cs",
        // DreamTravel component test fixture setup
        "ComponentTestsFixture.cs",
        "ApiFixture.cs",
        "WorkerFixture.cs",
    };

    private static readonly Regex TestHostPattern = new(
        @"\bnew\s+(WebApplicationFactory|APIFixture)",
        RegexOptions.Compiled);

    [Test]
    public void TestHost_Instantiation_Only_In_Approved_Files()
    {
        var testsDir = Path.Combine(RepoRoot, "tests");
        var sampleTestsDir = Path.Combine(RepoRoot, "sample-tale-code-apps");

        var allTestFiles = Directory.GetFiles(testsDir, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(sampleTestsDir, "*.cs", SearchOption.AllDirectories));

        var violations = new List<string>();

        foreach (var file in allTestFiles)
        {
            var fileName = Path.GetFileName(file);
            if (ApprovedFiles.Contains(fileName)) continue;

            var content = File.ReadAllText(file);
            if (TestHostPattern.IsMatch(content))
            {
                var relativePath = Path.GetRelativePath(RepoRoot, file);
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            "WebApplicationFactory/APIFixture must be instantiated only in approved fixture files. " +
            $"Violations: [{string.Join(", ", violations)}]. " +
            "Either use an existing fixture base class, or add the file to ApprovedFiles with a reason.");
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

