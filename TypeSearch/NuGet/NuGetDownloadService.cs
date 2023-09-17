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
using IMSLogger = Microsoft.Extensions.Logging.ILogger<TypeSearch.NuGet.NuGetDownloadService>;

namespace TypeSearch.NuGet;

public class NuGetDownloadService
{
	private readonly IMSLogger? logger;
	private readonly SourceCacheContext cache;
	private readonly SourceRepository repository;

	private Lazy<Task<DownloadResource>> downloadResource;
	private Lazy<INuGetLogger> nuGetLogger;

	public NuGetDownloadService(SourceCacheContext cache, SourceRepository repository) : this(null, cache, repository) { }

	public NuGetDownloadService(IMSLogger? logger, SourceCacheContext cache, SourceRepository repository)
	{
		this.logger = logger;
		this.cache = cache;
		this.repository = repository;

		downloadResource = new Lazy<Task<DownloadResource>>(this.repository.GetResourceAsync<DownloadResource>);
		nuGetLogger = new Lazy<INuGetLogger>(() =>
			logger is not null
				? new NuGetLogger(logger)
				: NullLogger.Instance);
	}

	public async Task<FetchPackageResult?> FetchPackage(string name, Version version, CancellationToken cancellationToken)
	{
		using var ps = logger?.BeginScope(new { name, version });

		logger?.LogTrace("Fetching download resource");
		var downloadResourceValue = await downloadResource.Value;
		logger?.LogTrace("Fetching NuGet package information");
		var result = await downloadResourceValue.GetDownloadResourceResultAsync(
			new PackageIdentity(name, version.ToNuGetVersion()),
			new PackageDownloadContext(cache),
			Path.GetTempPath(),
			nuGetLogger.Value,
			cancellationToken);
		logger?.LogTrace("Reading library items");
		var frameworks = await result.PackageReader.GetLibItemsAsync(cancellationToken);

		if (cancellationToken.IsCancellationRequested)
		{
			logger?.LogTrace("Downloading NuGet package was canceled");
			return null;
		}

		if (frameworks?.FirstOrDefault() is not FrameworkSpecificGroup frameworkGroup)
		{
			logger?.LogWarning("No framework found for NuGet package");
			return null;
		}


		var zip = new ZipArchive(result.PackageStream);

		if (await OpenFirstItem(zip, frameworkGroup.Items, ".dll", cancellationToken) is not Stream assembly)
		{
			logger?.LogWarning("No assembly found for NuGet package");
			return null;
		}
		logger?.LogTrace("Assembly found for NuGet package");

		var documentation = await OpenFirstItem(zip, frameworkGroup.Items, ".xml", cancellationToken);
		logger?.LogTrace("Documentation found for NuGet package: {WasFound}", documentation is not null);

		return new FetchPackageResult(assembly, documentation);

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

public record FetchPackageResult(Stream Assembly, Stream? Documentation) : IAsyncDisposable, IDisposable
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
