using System.Xml;

namespace TypeSearch.Documentation;

public record DocumentationText(string Text)
{
	public static async Task<DocumentationText?> ReadAsync(XmlReader reader) =>
		new(await reader.ReadInnerXmlAsync());
}
