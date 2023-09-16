using System;
using System.Collections.Generic;
using System.Linq;
using TypeSearch.Domain;

namespace TypeSearch.Reflection;

public static class TypeExtensions
{
	public static IEnumerable<Type> EnumerateBaseClasses(this Type classType)
	{
		for (var typeDefinition = classType;
			typeDefinition is not null;
			typeDefinition = typeDefinition.BaseType)
		{
			yield return typeDefinition;
		}
	}

	public static bool IsAssignableTo(this Type type, IType target) =>
		target switch
		{
			ReflectionType reflectionTarget => type.IsAssignableTo(reflectionTarget.Type),
			SearchType searchType => searchType.PossibleTypes.Any(type.IsAssignableTo),
			_ => type.EnumerateBaseClasses()
					.Any(t => string.Equals(t.Name, target.Name, StringComparison.OrdinalIgnoreCase))
				|| type.GetInterfaces()
					.Any(i => string.Equals(i.Name, target.Name, StringComparison.OrdinalIgnoreCase))
		};
}
