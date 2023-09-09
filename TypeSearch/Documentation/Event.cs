using System.Xml;

namespace TypeSearch.Documentation;

public class Event : Member
{
	public new static async Task<Event> ReadAsync(XmlReader reader, MemberIdentifier identifier)
	{
		DocumentationText? summary = null;
		DocumentationText? remarks = null;

		while (await reader.ReadAsync())
		{
			if (reader is { NodeType: XmlNodeType.Element, Name: "summary" })
			{
				summary = await DocumentationText.ReadAsync(reader);
			}
			else if (reader is { NodeType: XmlNodeType.Element, Name: "remarks" })
			{
				remarks = await DocumentationText.ReadAsync(reader);
			}
			else if (reader is { NodeType: XmlNodeType.EndElement })
			{
				break;
			}
		}

		return new Event(identifier, summary, remarks);
	}

	public Event(
		MemberIdentifier identifier,
		DocumentationText? summary,
		DocumentationText? remarks) : base(identifier, summary, remarks)
	{
	}
}
