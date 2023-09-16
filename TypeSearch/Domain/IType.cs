namespace TypeSearch.Domain;

public interface IType
{
	string Name { get; }

	bool IsAssignableTo(IType target);
}

public interface IMaterializedType : IType
{
	string FullName { get; }
}
