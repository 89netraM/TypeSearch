using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Serilog;
using Serilog.Formatting.Compact;
using TypeSearch.Domain;
using TypeSearch.NuGet;
using TypeSearch.Reflection;

var services = new ServiceCollection();
services.AddLogging(c => c.AddSerilog(
	new LoggerConfiguration()
		.WriteTo.Console(new RenderedCompactJsonFormatter())
		.CreateLogger()));
services.AddSingleton<SourceCacheContext>();
services.AddSingleton(Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json"));
services.AddSingleton<NuGetService>();
services.AddSingleton<PackageService>();

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<Program>>();
var packageService = provider.GetRequiredService<PackageService>();

var package = await LoadPackageFromArgument(args[0], CancellationToken.None);
if (package is null)
{
	logger.LogWarning("Package not found");
	return;
}

var results = Search.Lookup(
	package.Formulas,
	args
		.Skip(1)
		.SkipLast(1)
		.Select(package.TypeFromQuery)
		.ToArray(),
	package.TypeFromQuery(args.Last()));

foreach (var result in results)
{
	logger.LogInformation("Found formula: {DocumentationIdentifier}", result.DocumentationIdentifier);
}

async Task<Package?> LoadPackageFromArgument(string arg, CancellationToken ct)
{
	if (Path.Exists(arg))
	{
		logger.LogDebug("Loading package from file {FilePath}", arg);
		var package = new Package(Path.GetFileNameWithoutExtension(arg));
		using var fileStream = File.OpenRead(arg);
		package.AddAssembly(fileStream);
		return package;
	}

	if (arg.Split("/") is [var name, var versionString] && Version.TryParse(versionString, out var version))
	{
		logger.LogDebug("Loading package from NuGet {Name}/{Version}", name, version);
		return await packageService.FetchPackage(name, version, ct);
	}

	logger.LogWarning("First argument should either be a path to a .DLL or a NuGet package identifier (\"<Package.Name>/<Version>\"), was \"{Args0}\"", args[0]);
	return null;
}
