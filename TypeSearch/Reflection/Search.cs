using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using TypeSearch.Domain;

namespace TypeSearch.Reflection;

public static class Search
{
	private const BindingFlags AllDeclaredMembers =
		BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

	public static IEnumerable<IFormula> BuildFormulas(IEnumerable<Assembly> assemblies) =>
		assemblies.SelectMany(assembly => assembly
			.Modules
			.SelectMany(module => module
				.GetTypes()
				.Where(type => type.IsPublic)
				.SelectMany(type => type
					.GetFields(AllDeclaredMembers)
					.Select(FieldToFormula)
					.Concat(type.GetMethods(AllDeclaredMembers)
						.Select(MethodToFormula))
					.Concat(type.GetConstructors(AllDeclaredMembers & ~BindingFlags.DeclaredOnly)
						.Select(ConstructorToFormula))
					.Concat(type.GetProperties(AllDeclaredMembers)
						.Select(PropertyToFormula)))));

	private static IFormula FieldToFormula(FieldInfo field)
	{
		var declaringType = new ReflectionType(field.DeclaringType!);
		var name = field.Name;
		var fieldType = new ReflectionType(field.FieldType);

		if (field.IsStatic)
		{
			return new StaticField(declaringType, name, fieldType);
		}

		return new Field(declaringType, name, fieldType);
	}

	private static IFormula MethodToFormula(MethodInfo method)
	{
		var declaringType = new ReflectionType(method.DeclaringType!);
		var parameters = method.GetParameters()
			.Select(p => new ReflectionType(p.ParameterType))
			.ToArray();
		var name = method.Name;
		var returnType = new ReflectionType(method.ReturnType);

		if (method.IsStatic)
		{
			return new StaticMethod(declaringType, name, parameters, returnType);
		}

		return new Method(declaringType, name, parameters, returnType);
	}

	private static IFormula ConstructorToFormula(ConstructorInfo constructor)
	{
		var declaringType = new ReflectionType(constructor.DeclaringType!);
		var parameters = constructor.GetParameters()
			.Select(p => new ReflectionType(p.ParameterType))
			.ToArray();

		return new Constructor(declaringType, parameters);
	}

	private static IFormula PropertyToFormula(PropertyInfo property)
	{
		var declaringType = new ReflectionType(property.DeclaringType!);
		var name = property.Name;
		var propertyType = new ReflectionType(property.PropertyType);

		if (property.GetMethod!.IsStatic)
		{
			return new StaticProperty(declaringType, name, propertyType);
		}

		return new Property(declaringType, name, propertyType);
	}

	public static IType TypeFromQuery(IEnumerable<Assembly> assemblies, string query) =>
		new SearchType(
			query,
			assemblies.SelectMany(assembly => assembly
				.Modules
				.SelectMany(module => module
					.GetTypes()
					.Where(type => type.IsPublic && TypeMatchesQuery(type, query))))
				.ToArray());

	private static bool TypeMatchesQuery(Type type, string query)
	{
		using var provider = new CSharpCodeProvider();
		var prettyName = provider.GetTypeOutput(new CodeTypeReference(type));
		return string.Equals(type.Name, query, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(prettyName, query, StringComparison.OrdinalIgnoreCase);
	}
}
