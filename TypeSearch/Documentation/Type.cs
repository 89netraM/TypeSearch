using System.Xml;

namespace TypeSearch.Documentation;

public class Type : Member
{
	public new static async Task<Type?> ReadAsync(XmlReader reader, MemberIdentifier identifier)
	{
		DocumentationText? summary = null;
		DocumentationText? remarks = null;
		var typeParameters = new List<(string, DocumentationText?)>();

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
			else if (reader is { NodeType: XmlNodeType.Element, Name: "typeparam" } && reader.GetAttribute("name") is string typeName)
			{
				typeParameters.Add((typeName, await DocumentationText.ReadAsync(reader)));
			}
			else if (reader is { NodeType: XmlNodeType.EndElement })
			{
				break;
			}
		}

		return new Type(identifier, summary, remarks, typeParameters);
	}

	public IReadOnlyList<(string, DocumentationText?)> TypeParameters { get; }

	public Type(
		MemberIdentifier identifier,
		DocumentationText? summary,
		DocumentationText? remarks,
		IReadOnlyList<(string, DocumentationText?)> typeParameters) : base(identifier, summary, remarks)
	{
		TypeParameters = typeParameters;
	}
}
