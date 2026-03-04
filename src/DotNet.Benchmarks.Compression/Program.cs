using BenchmarkDotNet.Running;
using DotNet.Benchmarks.Compression.Benchmarks;

var repositoryRoot = FindRepositoryRoot();
var artifactsPath = Path.Combine(repositoryRoot, "results", "Compression", "artifacts");
Directory.CreateDirectory(artifactsPath);

var benchmarkArgs = args.Contains("--artifacts", StringComparer.OrdinalIgnoreCase)
	? args
	: args.Concat(["--artifacts", artifactsPath]).ToArray();

BenchmarkRunner.Run<ZstdVsBrotliBench>(args: benchmarkArgs);

return;

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
