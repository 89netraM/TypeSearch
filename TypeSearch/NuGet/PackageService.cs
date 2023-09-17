using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypeSearch.Reflection;

namespace TypeSearch.NuGet;

public class PackageService
{
	private readonly ILogger<PackageService>? logger;
	private readonly NuGetDownloadService downloadService;

	public PackageService(NuGetDownloadService downloadService) : this(null, downloadService) { }

	public PackageService(ILogger<PackageService>? logger, NuGetDownloadService downloadService)
	{
		this.logger = logger;
		this.downloadService = downloadService;
	}

	public async Task<Package?> FetchPackage(string name, Version version, CancellationToken cancellationToken)
	{
		using var ps = logger?.BeginScope(new { name, version });

		logger?.LogTrace("Downloading package");
		await using var result = await downloadService.FetchPackage(name, version, cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Package download canceled");
			return null;
		}

		if (result is null)
		{
			logger?.LogWarning("No package was downloaded");
			return null;
		}

		logger?.LogTrace("Package download succeeded");
		return Package.Load(name, result.Assembly);
	}
}
