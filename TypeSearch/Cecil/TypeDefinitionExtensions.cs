using System.Collections.Generic;
using Mono.Cecil;

namespace TypeSearch.Cecil;

public static class TypeDefinitionExtensions
{
	public static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition classType)
	{
		for (var typeDefinition = classType;
			typeDefinition is not null;
			typeDefinition = typeDefinition.BaseType?.Resolve())
		{
			yield return typeDefinition;
		}
	}
}