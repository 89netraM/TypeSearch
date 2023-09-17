using System;
using NuGet.Versioning;

namespace TypeSearch.NuGet;

public static class VersionExtensions
{
	public static NuGetVersion ToNuGetVersion(this Version version) =>
		new(version);
}
