using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using TypeSearch.Domain;

namespace TypeSearch.Cecil;

public static class Search
{
	public static IReadOnlyCollection<IFormula> BuildFormulas(AssemblyDefinition assembly) =>
		assembly.Modules
			.SelectMany(module => module
				.Types
				.Where(type => type.IsPublic)
				.SelectMany(type => type
					.Fields
						.Where(f => f.IsPublic)
						.Select(FieldToFormula)
					.Concat(type.Methods
						.Where(m => m.IsPublic)
						.Select(MethodToFormula))
					.Concat(type.Properties
						.Where(p => p.GetMethod?.IsPublic is true)
						.Select(PropertyToFormula))))
			.ToArray();

	private static IFormula FieldToFormula(FieldDefinition field)
	{
		var declaringType = new CecilType(field.DeclaringType);
		var name = field.Name;
		var fieldType = CecilType.FromTypeReference(field.FieldType);

		if (field.IsStatic)
		{
			return new StaticField(declaringType, name, fieldType);
		}

		return new Field(declaringType, name, fieldType);
	}

	private static IFormula MethodToFormula(MethodDefinition method)
	{
		var declaringType = new CecilType(method.DeclaringType);
		var parameters = method.Parameters
			.Select(p => CecilType.FromTypeReference(p.ParameterType))
			.ToArray();

		if (method.IsConstructor)
		{
			return new Constructor(declaringType, parameters);
		}

		var name = method.Name;
		var returnType = CecilType.FromTypeReference(method.ReturnType);

		if (method.IsStatic)
		{
			return new StaticMethod(declaringType, name, parameters, returnType);
		}

		return new Method(declaringType, name, parameters, returnType);
	}

	private static IFormula PropertyToFormula(PropertyDefinition property)
	{
		var declaringType = new CecilType(property.DeclaringType);
		var name = property.Name;
		var propertyType = CecilType.FromTypeReference(property.PropertyType);

		if (property.GetMethod.IsStatic)
		{
			return new StaticProperty(declaringType, name, propertyType);
		}

		return new Property(declaringType, name, propertyType);
	}
}
