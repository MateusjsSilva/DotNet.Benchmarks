using BenchmarkDotNet.Running;
using DotNet.Benchmarks.GuidGen.Scenarios;

var repositoryRoot = FindRepositoryRoot();
var resultsRoot = Path.Combine(repositoryRoot, "results", "GuidGen");
Directory.CreateDirectory(resultsRoot);

if (args.Contains("--scenarios-convergence", StringComparer.OrdinalIgnoreCase))
{
    var reportPath = Path.Combine(resultsRoot, "GuidConvergenceReport.md");
    return GuidConvergenceRunner.RunAndSave(Console.Out, reportPath);
}

if (args.Contains("--scenarios-migration", StringComparer.OrdinalIgnoreCase))
{
    var reportPath = Path.Combine(resultsRoot, "GuidMigrationReport.md");
    return GuidMigrationScenarioRunner.RunAndSave(Console.Out, reportPath);
}

if (args.Contains("--scenarios-multi", StringComparer.OrdinalIgnoreCase))
{
    var reportPath = Path.Combine(resultsRoot, "GuidScenarioReport-Multi.md");
    return GuidScenarioRunner.RunMultiScaleAndSave(Console.Out, reportPath);
}

if (args.Contains("--scenarios", StringComparer.OrdinalIgnoreCase))
{
    var reportPath = Path.Combine(resultsRoot, "GuidScenarioReport.md");
    return GuidScenarioRunner.RunAndSave(Console.Out, reportPath);
}

var artifactsPath = Path.Combine(resultsRoot, "artifacts");
Directory.CreateDirectory(artifactsPath);

var benchmarkArgs = args.Contains("--artifacts", StringComparer.OrdinalIgnoreCase)
    ? args
    : args.Concat(["--artifacts", artifactsPath]).ToArray();

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(benchmarkArgs);
return 0;

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "DotNet.Benchmarks.slnx")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return Directory.GetCurrentDirectory();
}
