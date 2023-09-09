using System.Xml;

namespace TypeSearch.Documentation;

public abstract class Member
{
	public static async Task<Member?> ReadAsync(XmlReader reader, MemberIdentifier identifier) =>
		identifier.Kind switch
		{
			MemberKind.Event => await Event.ReadAsync(reader, identifier),
			MemberKind.Field => await Field.ReadAsync(reader, identifier),
			MemberKind.Method => await Method.ReadAsync(reader, identifier),
			MemberKind.Property => await Property.ReadAsync(reader, identifier),
			MemberKind.Type => await Type.ReadAsync(reader, identifier),
			_ => null,
		};

	public MemberIdentifier Identifier { get; }
	public DocumentationText? Summary { get; }
	public DocumentationText? Remarks { get; }

	protected Member(MemberIdentifier identifier, DocumentationText? summary, DocumentationText? remarks)
	{
		Identifier = identifier;
		Summary = summary;
		Remarks = remarks;
	}
}
