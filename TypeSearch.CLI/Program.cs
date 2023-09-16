using System;
using System.Linq;
using System.Text.Json;

var assemblies = new[] { typeof(string), typeof(Enumerable) }
	.Select(t => t.Assembly)
	.ToArray();

var formulas = TypeSearch.Reflection.Search.BuildFormulas(assemblies);

var results = TypeSearch.Domain
	.Search
	.Lookup(
		formulas,
		new[] { "char", "int" }
			.Select(q => TypeSearch.Reflection.Search.TypeFromQuery(assemblies, q))
			.ToArray(),
		TypeSearch.Reflection.Search.TypeFromQuery(assemblies, "string"));

foreach (var result in results)
{
	Console.WriteLine(result.GetType());
	Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
	{
		WriteIndented = true,
	}));
}
