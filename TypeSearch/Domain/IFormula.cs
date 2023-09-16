using System.Collections.Generic;

namespace TypeSearch.Domain;

public interface IFormula
{
	IReadOnlyCollection<IMaterializedType> Ingredients { get; }

	IMaterializedType Result { get; }

	string DocumentationIdentifier { get; }
}
