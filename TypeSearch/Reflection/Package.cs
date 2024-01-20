using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CSharp;
using TypeSearch.Domain;

namespace TypeSearch.Reflection;

public class Package : IDisposable
{
	private const BindingFlags PublicDeclaredMembers =
		BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

	private readonly AssemblyLoadContext context;

	private List<IFormula> formulas = new List<IFormula>();
	public IReadOnlyCollection<IFormula> Formulas => formulas;

	public Package(string name)
	{
		context = new AssemblyLoadContext(name, true);
	}

	public void AddAssembly(Stream stream)
	{
		var assembly = context.LoadFromStream(stream);

		formulas.AddRange(BuildFormulas(assembly));

		static IEnumerable<IFormula> BuildFormulas(Assembly assembly) =>
			assembly
				.Modules
				.SelectMany(module => module
					.GetTypes()
					.Where(type => type.IsPublic)
					.SelectMany(type => type
						.GetFields(PublicDeclaredMembers)
						.Select(FieldToFormula)
						.Concat(type.GetMethods(PublicDeclaredMembers)
							.Select(MethodToFormula))
						.Concat(type.GetConstructors(PublicDeclaredMembers & ~BindingFlags.DeclaredOnly)
							.Select(ConstructorToFormula))
						.Concat(type.GetProperties(PublicDeclaredMembers)
							.Select(PropertyToFormula))));

		static IFormula FieldToFormula(FieldInfo field)
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

		static IFormula MethodToFormula(MethodInfo method)
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

		static IFormula ConstructorToFormula(ConstructorInfo constructor)
		{
			var declaringType = new ReflectionType(constructor.DeclaringType!);
			var parameters = constructor.GetParameters()
				.Select(p => new ReflectionType(p.ParameterType))
				.ToArray();

			return new Constructor(declaringType, parameters);
		}

		static IFormula PropertyToFormula(PropertyInfo property)
		{
			var declaringType = new ReflectionType(property.DeclaringType!);
			var name = property.Name;
			var propertyType = new ReflectionType(property.PropertyType);

			if (property.GetMethod?.IsStatic is true)
			{
				return new StaticProperty(declaringType, name, propertyType);
			}

			return new Property(declaringType, name, propertyType);
		}
	}

	public IType TypeFromQuery(string query)
	{
		return new SearchType(
			query,
			context.Assemblies
				.SelectMany(assembly => assembly
					.Modules
					.SelectMany(module => module
						.GetTypes()
						.Where(type => type.IsPublic && TypeMatchesQuery(type, query))))
				.ToArray());

		static bool TypeMatchesQuery(Type type, string query)
		{
			using var provider = new CSharpCodeProvider();
			var prettyName = provider.GetTypeOutput(new CodeTypeReference(type));
			return string.Equals(type.Name, query, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(prettyName, query, StringComparison.OrdinalIgnoreCase);
		}
	}

	public void Dispose()
	{
		context.Unload();
	}
}
