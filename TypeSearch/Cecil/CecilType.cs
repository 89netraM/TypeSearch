using System;
using System.Linq;
using Mono.Cecil;
using TypeSearch.Domain;

namespace TypeSearch.Cecil;

public class CecilType : IMaterializedType
{
	public static CecilType FromTypeReference(TypeReference typeReference) =>
		typeReference.Resolve() is TypeDefinition typeDefinition
			? new CecilType(typeDefinition)
			: new DummyType();

	public virtual string Name => typeDefinition.Name;

	public virtual string FullName => typeDefinition.FullName;

	private readonly TypeDefinition typeDefinition;

	public CecilType(TypeDefinition typeDefinition) =>
		this.typeDefinition = typeDefinition;

	public bool IsAssignableTo(IType target) =>
		typeDefinition
			.EnumerateBaseClasses()
			.Any(t =>
				string.Equals(t.Name, target.Name, StringComparison.OrdinalIgnoreCase) ||
				t.Interfaces.Any(i =>
					string.Equals(i.InterfaceType.Name, target.Name, StringComparison.OrdinalIgnoreCase)));

	private class DummyType : CecilType
	{
		public override string Name => string.Empty;
		public override string FullName => string.Empty;

		public DummyType() : base(null!) { }
	}
}
