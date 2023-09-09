using System.Xml;

namespace TypeSearch.Documentation;

public class Property : Member
{
	public new static async Task<Property?> ReadAsync(XmlReader reader, MemberIdentifier identifier)
	{
		DocumentationText? summary = null;
		DocumentationText? remarks = null;
		DocumentationText? value = null;

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
			else if (reader is { NodeType: XmlNodeType.Element, Name: "value" })
			{
				value = await DocumentationText.ReadAsync(reader);
			}
			else if (reader is { NodeType: XmlNodeType.EndElement })
			{
				break;
			}
		}

		return new Property(identifier, summary, remarks, value);
	}

	public DocumentationText? Value { get; }

	public Property(
		MemberIdentifier identifier,
		DocumentationText? summary,
		DocumentationText? remarks,
		DocumentationText? value) : base(identifier, summary, remarks)
	{
		Value = value;
	}
}
