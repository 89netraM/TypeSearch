using System.Collections.Generic;
using System.Linq;

namespace TypeSearch.Domain;

public static class Search
{
	public static IEnumerable<TFormula> Lookup<TFormula, TFrom>(
		IEnumerable<TFormula> formulas,
		IReadOnlyCollection<TFrom> from,
		IType to)
	where TFormula : IFormula
	where TFrom : IType =>
		formulas
			.Where(f =>
				ResultMatches(to, f.Result) &&
				IngredientsMatches(from, f.Ingredients));

	private static bool ResultMatches(IType to, IType result) =>
		result.IsAssignableTo(to);

	private static bool IngredientsMatches<TFrom, TIngredient>(
		IReadOnlyCollection<TFrom> from,
		IReadOnlyCollection<TIngredient> ingredients)
	where TFrom : IType
	where TIngredient : IType
	{
		if (ingredients.Count != from.Count)
		{
			return false;
		}
		
		if (ingredients.Count == 0 && from.Count == 0)
		{
			return true;
		}

		var matchedIngredients = new HashSet<int>();
		return from.All(fromType => MatchAgainstIngredient(fromType));

		bool MatchAgainstIngredient(IType fromType)
		{
			foreach (var (ingredient, i) in ingredients.Zip(Enumerable.Range(0, ingredients.Count)))
			{
				if (fromType.IsAssignableTo(ingredient) && matchedIngredients.Add(i))
				{
					return true;
				}
			}

			return false;
		}
	}
}
