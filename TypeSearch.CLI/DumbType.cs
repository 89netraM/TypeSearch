using System;
using TypeSearch.Domain;

namespace TypeSearch.CLI;

public record DumbType(string Name) : IType
{
	public bool IsAssignableTo(IType target) =>
		string.Equals(Name, target.Name, StringComparison.OrdinalIgnoreCase);
}
