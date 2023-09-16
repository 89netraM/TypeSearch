using System;
using System.Collections.Generic;
using System.Linq;
using TypeSearch.Domain;

namespace TypeSearch.Reflection;

public record SearchType(string Name, IReadOnlyCollection<Type> PossibleTypes) : IType
{
	public bool IsAssignableTo(IType target) =>
		PossibleTypes.Any(type => type.IsAssignableTo(target));
}
