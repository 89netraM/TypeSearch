using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypeSearch.Reflection;

namespace TypeSearch.NuGet;

public class PackageService
{
	private readonly ILogger<PackageService>? logger;
	private readonly NuGetService nuGetService;

	public PackageService(NuGetService downloadService) : this(null, downloadService) { }

	public PackageService(ILogger<PackageService>? logger, NuGetService nuGetService)
	{
		this.logger = logger;
		this.nuGetService = nuGetService;
	}

	public async Task<Package?> FetchPackage(string name, Version version, CancellationToken cancellationToken)
	{
		using var ps = logger?.BeginScope(new { name, version });

		var dependencyTree = await nuGetService.FetchDependencyTree(name, version, cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Fetching package was canceled");
			return null;
		}

		if (dependencyTree is null)
		{
			logger?.LogTrace("No dependency tree returned");
			return null;
		}

		logger?.LogTrace("Creating package container");
		var package = new Package(name);

		if (!await TraverseDependencies(package, dependencyTree, cancellationToken))
		{
			logger?.LogTrace("Disposing package container");
			package.Dispose();
			return null;
		}

		logger?.LogTrace("Returning package container");
		return package;
	}

	private async Task<bool> TraverseDependencies(Package package, DependencyTree dependencyTree, CancellationToken cancellationToken)
	{
		var dependenciesSuccess = await dependencyTree
			.Dependencies
			.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(async (d, ct) => await TraverseDependencies(package, d, ct))
			.AggregateAsync(true, (a, b) => a && b, cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Downloading package dependency was canceled");
			return false;
		}

		if (!dependenciesSuccess)
		{
			return false;
		}

		logger?.BeginScope(new { dependencyTree.Name, dependencyTree.Version });

		logger?.LogTrace("Fetching package");
		var packageResult = await nuGetService.FetchPackage(dependencyTree.Name, dependencyTree.Version, cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Downloading package dependency was canceled");
			return false;
		}

		if (packageResult is FetchPackageResult.NotFound)
		{
			logger?.LogTrace("Package not found");
			return false;
		}

		if (packageResult is FetchPackageResult.Empty)
		{
			logger?.LogTrace("Package was empty");
			return true;
		}

		if (packageResult is not FetchPackageResult.Found(var assembly, _))
		{
			logger?.LogError("Unexpected package result type \"{PackageResultType}\", where are my discriminated unions!", packageResult.GetType().Name);
			return false;
		}

		logger?.LogTrace("Adding package assembly");
		package.AddAssembly(assembly);

		return true;
	}
}
