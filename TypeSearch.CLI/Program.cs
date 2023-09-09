using System;
using System.Linq;
using TypeSearch;

var result = Search.Lookup(new[] { typeof(Enumerable).Assembly, typeof(string).Assembly }, new[] { typeof(string), typeof(string) }, typeof(bool));

Console.WriteLine("Found:");
foreach (var (item, _) in result.OrderByDescending(p => p.Item2))
{
	Console.WriteLine("\t" + item);
}
