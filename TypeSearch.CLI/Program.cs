using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using TypeSearch.Domain;
using TypeSearch.NuGet;

var services = new ServiceCollection();
services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
services.AddSingleton<SourceCacheContext>();
services.AddSingleton(Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json"));
services.AddSingleton<NuGetDownloadService>();
services.AddSingleton<PackageService>();

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<Program>>();
var packageService = provider.GetRequiredService<PackageService>();

if (args[0].Split("/") is not [var name, var versionString])
{
	logger.LogWarning("First argument didn't match \"<Package.Name>/<Version>\" format, was \"{Args0}\"", args[0]);
	return;
}

if (!Version.TryParse(versionString, out var version))
{
	logger.LogWarning("Version string \"{VersionString}\" was not a valid version", versionString);
	return;
}

var package = await packageService.FetchPackage(name, version, CancellationToken.None);
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
