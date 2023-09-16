using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeSearch.Domain;

public abstract record MethodBase(
	IMaterializedType DeclaringType,
	IReadOnlyCollection<IMaterializedType> Ingredients,
	IMaterializedType Result,
	string DocumentationIdentifier) : IFormula
{
	protected internal static string DocumentifyParameters(IReadOnlyCollection<IMaterializedType> parameters)
	{
		var sb = new StringBuilder();

		foreach (var parameter in parameters)
		{
			sb.Append(sb.Length == 0 ? '(' : ',');

			sb.Append(parameter.FullName);
		}

		if (sb.Length > 0)
		{
			sb.Append(')');
		}

		return sb.ToString();
	}
}

public record Method(
		IMaterializedType DeclaringType,
		string Name,
		IReadOnlyCollection<IMaterializedType> Parameters,
		IMaterializedType ReturnType) :
	MethodBase(
		DeclaringType,
		Parameters.Prepend(DeclaringType).ToArray(),
		ReturnType,
		$"M:{DeclaringType.FullName}.{Name}{DocumentifyParameters(Parameters)}");

public record StaticMethod(
		IMaterializedType DeclaringType,
		string Name,
		IReadOnlyCollection<IMaterializedType> Parameters,
		IMaterializedType ReturnType) :
	MethodBase(
		DeclaringType,
		Parameters,
		ReturnType,
		$"M:{DeclaringType.FullName}.{Name}{DocumentifyParameters(Parameters)}");

public record Constructor(
		IMaterializedType DeclaringType,
		IReadOnlyCollection<IMaterializedType> Parameters) :
	MethodBase(
		DeclaringType,
		Parameters.ToArray(),
		DeclaringType,
		$"M:{DeclaringType.FullName}.#ctor{DocumentifyParameters(Parameters)}");
