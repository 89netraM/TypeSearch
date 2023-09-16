using System;
using System.Linq;
using System.Text.Json;
using Mono.Cecil;
using TypeSearch.Cecil;
using TypeSearch.CLI;
using TypeSearch.Domain;

var formulas = new[] { typeof(string), typeof(Enumerable) }
	.Select(t => AssemblyDefinition.ReadAssembly(t.Assembly.Location))
	.SelectMany(TypeSearch.Cecil.Search.BuildFormulas)
	.ToArray();

var results = TypeSearch.Domain
	.Search
	.Lookup(
		formulas, 
		new[] { "String", "Char" }
			.Select(text => new DumbType(text))
			.ToArray(),
		new DumbType("Boolean"));

foreach (var result in results)
{
	Console.WriteLine(result.GetType());
	Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
	{
		WriteIndented = true,
	}));
}
