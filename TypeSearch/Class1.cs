using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace TypeSearch;

public class Class1
{
	public static async Task Fetch()
	{
		var logger = NullLogger.Instance;
		using var cache = new SourceCacheContext();
		var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var resource = await repository.GetResourceAsync<DownloadResource>();
		using var result = await resource.GetDownloadResourceResultAsync(
			new("Newtonsoft.Json", new(13, 0, 3)),
			new(cache),
			Path.GetTempPath(),
			logger,
			CancellationToken.None);
		var libs = await result.PackageReader.GetLibItemsAsync(CancellationToken.None);
		var types = libs.Skip(3).First().Items.First(p => Path.GetExtension(p) == ".dll");
		var zip = new ZipArchive(result.PackageStream);
		var assemblyFile = zip.GetEntry(types) ?? throw new Exception();
		await using var assemblyStream = assemblyFile.Open();
		using var ms = new MemoryStream();
		await assemblyStream.CopyToAsync(ms);
		var assembly = Assembly.Load(ms.ToArray());
	}
}
