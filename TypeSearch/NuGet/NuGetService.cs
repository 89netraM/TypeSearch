using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using INuGetLogger = NuGet.Common.ILogger;
using IMSLogger = Microsoft.Extensions.Logging.ILogger<TypeSearch.NuGet.NuGetService>;
using NuGet.Frameworks;

namespace TypeSearch.NuGet;

public class NuGetService
{
	private static Lazy<NuGetFramework> targetFramework = new Lazy<NuGetFramework>(() =>
		AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName is string name
			? NuGetFramework.Parse(name)
			: NuGetFramework.AgnosticFramework);

	private readonly IMSLogger? logger;
	private readonly SourceCacheContext cache;
	private readonly SourceRepository repository;

	private Lazy<Task<DownloadResource>> downloadResource;
	private Lazy<Task<DependencyInfoResource>> dependencyInfoResource;
	private Lazy<INuGetLogger> nuGetLogger;

	public NuGetService(SourceCacheContext cache, SourceRepository repository) : this(null, cache, repository) { }

	public NuGetService(IMSLogger? logger, SourceCacheContext cache, SourceRepository repository)
	{
		this.logger = logger;
		this.cache = cache;
		this.repository = repository;

		downloadResource = new Lazy<Task<DownloadResource>>(this.repository.GetResourceAsync<DownloadResource>);
		dependencyInfoResource = new Lazy<Task<DependencyInfoResource>>(this.repository.GetResourceAsync<DependencyInfoResource>);
		nuGetLogger = new Lazy<INuGetLogger>(() =>
			logger is not null
				? new NuGetLogger(logger)
				: NullLogger.Instance);
	}

	public async Task<DependencyTree?> FetchDependencyTree(string name, Version version, CancellationToken cancellationToken)
	{
		using var ps = logger?.BeginScope(new { name, version });

		var packageIdentity = new PackageIdentity(name, version.ToNuGetVersion());

		logger?.LogTrace("Fetching dependency info resource");
		var dependencyInfoResourceValue = await dependencyInfoResource.Value;
		logger?.LogTrace("Fetching NuGet package dependency information");
		var dependencyResult = await dependencyInfoResourceValue.ResolvePackage(
			packageIdentity,
			targetFramework.Value,
			cache,
			nuGetLogger.Value,
			cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Downloading NuGet package dependency information was canceled");
			return null;
		}

		logger?.LogTrace("Fetching child dependencies");
		using var innerCT = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		var dependencies = await dependencyResult
			.Dependencies
			.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(async (d, ct) =>
				{
					var tree = await FetchDependencyTree(d.Id, d.VersionRange.MinVersion?.Version!, ct);
					if (tree is null)
					{
						logger?.LogTrace("Downloading child dependency ({DependencyName}) failed, canceling all downloads", d.Id);
						innerCT.Cancel();
					}
					return tree;
				})
			.ToArrayAsync(innerCT.Token);

		if (innerCT.IsCancellationRequested)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				logger?.LogTrace("Downloading NuGet package dependency information was canceled");
			}
			return null;
		}

		return new DependencyTree(name, version, dependencies!);
	}

	public async Task<FetchPackageResult> FetchPackage(string name, Version version, CancellationToken cancellationToken)
	{
		using var ps = logger?.BeginScope(new { name, version });

		var packageIdentity = new PackageIdentity(name, version.ToNuGetVersion());

		logger?.LogTrace("Fetching download resource");
		var downloadResourceValue = await downloadResource.Value;
		logger?.LogTrace("Fetching NuGet package information");
		using var result = await downloadResourceValue.GetDownloadResourceResultAsync(
			packageIdentity,
			new PackageDownloadContext(cache),
			Path.GetTempPath(),
			nuGetLogger.Value,
			cancellationToken);
		logger?.LogTrace("Reading library items");
		var frameworks = await result.PackageReader.GetLibItemsAsync(cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Downloading NuGet package was canceled");
			return new FetchPackageResult.NotFound();
		}

		if (frameworks?.FirstOrDefault() is not FrameworkSpecificGroup frameworkGroup)
		{
			logger?.LogWarning("No framework found for NuGet package");
			return new FetchPackageResult.Empty();
		}

		var zip = new ZipArchive(result.PackageStream);

		if (await OpenFirstItem(zip, frameworkGroup.Items, ".dll", cancellationToken) is not Stream assembly)
		{
			logger?.LogWarning("No assembly found for NuGet package");
			return new FetchPackageResult.Empty();
		}
		logger?.LogTrace("Assembly found for NuGet package");

		var documentation = await OpenFirstItem(zip, frameworkGroup.Items, ".xml", cancellationToken);
		logger?.LogTrace("Documentation found for NuGet package: {WasFound}", documentation is not null);

		return new FetchPackageResult.Found(assembly, documentation);

		static async Task<Stream?> OpenFirstItem(ZipArchive zip, IEnumerable<string> items, string extension, CancellationToken cancellationToken)
		{
			var item = items.FirstOrDefault(p => Path.GetExtension(p) == extension);
			if (item is null || zip.GetEntry(item) is not ZipArchiveEntry archiveEntry)
			{
				return null;
			}

			await using var zipStream = archiveEntry.Open();
			var memoryStream = new MemoryStream();
			await zipStream.CopyToAsync(memoryStream, cancellationToken);
			memoryStream.Position = 0;
			return memoryStream;
		}
	}
}

public abstract record FetchPackageResult
{
	public record Found(Stream Assembly, Stream? Documentation) : FetchPackageResult, IAsyncDisposable, IDisposable
	{
		public async ValueTask DisposeAsync()
		{
			await Assembly.DisposeAsync();
			if (Documentation is not null)
			{
				await Documentation.DisposeAsync();
			}
		}

		public void Dispose()
		{
			Assembly.Dispose();
			Documentation?.Dispose();
		}
	}

	public record NotFound : FetchPackageResult;

	public record Empty : FetchPackageResult;
}

public record DependencyTree(string Name, Version Version, IReadOnlyCollection<DependencyTree> Dependencies);
