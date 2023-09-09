using System.Xml;

namespace TypeSearch.Documentation;

public class Method : Member
{
	public new static async Task<Method?> ReadAsync(XmlReader reader, MemberIdentifier identifier)
	{
		DocumentationText? summary = null;
		DocumentationText? remarks = null;
		var parameters = new List<(string, DocumentationText?)>();
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
			else if (reader is { NodeType: XmlNodeType.Element, Name: "param" } && reader.GetAttribute("name") is string name)
			{
				parameters.Add((name, await DocumentationText.ReadAsync(reader)));
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

		return new Method(identifier, summary, remarks, parameters, typeParameters);
	}

	public IReadOnlyList<(string, DocumentationText?)> Parameters { get; }
	public IReadOnlyList<(string, DocumentationText?)> TypeParameters { get; }

	public Method(
		MemberIdentifier identifier,
		DocumentationText? summary,
		DocumentationText? remarks,
		IReadOnlyList<(string, DocumentationText?)> parameters,
		IReadOnlyList<(string, DocumentationText?)> typeParameters) : base(identifier, summary, remarks)
	{
		Parameters = parameters;
		TypeParameters = typeParameters;
	}
}
