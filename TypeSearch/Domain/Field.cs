using System;
using System.Collections.Generic;

namespace TypeSearch.Domain;

public abstract record FieldBase(
	IMaterializedType DeclaringType,
	IReadOnlyCollection<IMaterializedType> Ingredients,
	string Name,
	IMaterializedType Result) : IFormula
{
	public string DocumentationIdentifier { get; } = $"F:{DeclaringType.FullName}.{Name}";
}

public record Field(IMaterializedType DeclaringType, string Name, IMaterializedType FieldType) :
	FieldBase(DeclaringType, new[] { DeclaringType }, Name, FieldType);

public record StaticField(IMaterializedType DeclaringType, string Name, IMaterializedType FieldType) :
	FieldBase(DeclaringType, Array.Empty<IMaterializedType>(), Name, FieldType);
