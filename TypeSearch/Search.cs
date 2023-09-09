using System.Reflection;

namespace TypeSearch;

public static class Search
{
	public static IEnumerable<(string, int)> Lookup(IEnumerable<Assembly> assemblies, IEnumerable<Type> from, Type to)
	{
		var have = from.ToList();
		var used = new List<Type>();

		foreach (var assembly in assemblies)
		{
			foreach (var type in assembly.GetExportedTypes())
			{
				foreach (var method in type.GetMethods())
				{
					if (method.ReturnType != to)
					{
						continue;
					}

					if (!method.IsStatic)
					{
						var haveType = have.FirstOrDefault(t => t.IsAssignableTo(type));
						if (haveType is null)
						{
							continue;
						}
						have.Remove(haveType);
						used.Add(haveType);
					}

					var parameters = method.GetParameters();
					foreach (var param in parameters)
					{
						var haveType = have.FirstOrDefault(t => t.IsAssignableTo(param.ParameterType));
						if (haveType is not null)
						{
							have.Remove(haveType);
							used.Add(haveType);
						}
					}

					if (used.Count == parameters.Length + (method.IsStatic ? 0 : 1))
					{
						yield return ($"{type.FullName}.{method.Name}({string.Join(",", parameters.Select(p => p.ParameterType.FullName))})", used.Count);
					}

					have.AddRange(used);
					used.Clear();
				}
			}
		}
	}
}
