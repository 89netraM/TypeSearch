using System;
using TypeSearch.Domain;

namespace TypeSearch.Reflection;

public class ReflectionType : IMaterializedType
{
	public virtual string Name => Type.Name;

	public virtual string FullName => Type.FullName!;

	public Type Type { get; }

	public ReflectionType(Type type) =>
		this.Type = type;

	public bool IsAssignableTo(IType target) =>
		Type.IsAssignableTo(target);
}
