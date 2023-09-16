using System;
using System.Collections.Generic;

namespace TypeSearch.Domain;

public abstract record PropertyBase(
	IMaterializedType DeclaringType,
	IReadOnlyCollection<IMaterializedType> Ingredients,
	string Name,
	IMaterializedType Result) : IFormula
{
	public string DocumentationIdentifier { get; } = $"P:{DeclaringType.FullName}.{Name}";
}

public record Property(IMaterializedType DeclaringType, string Name, IMaterializedType PropertyType) :
	PropertyBase(DeclaringType, new[] { DeclaringType }, Name, PropertyType);

public record StaticProperty(IMaterializedType DeclaringType, string Name, IMaterializedType PropertyType) :
	PropertyBase(DeclaringType, Array.Empty<IMaterializedType>(), Name, PropertyType);
