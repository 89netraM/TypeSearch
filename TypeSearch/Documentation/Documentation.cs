using System.Xml;

namespace TypeSearch.Documentation;

public class Documentation
{
	public static async Task<Documentation?> ReadFromAsync(Stream stream)
	{
		var settings = new XmlReaderSettings
		{
			Async = true,
		};
		using var reader = XmlReader.Create(stream, settings);
		return await ReadFromAsync(reader);
	}

	public static async Task<Documentation?> ReadFromAsync(XmlReader reader)
	{
		while (await reader.ReadAsync())
		{
			if (reader is { NodeType: XmlNodeType.Element, Name: "members" })
			{
				return await ReadMembers(reader);
			}
		}

		return null;

		static async Task<Documentation?> ReadMembers(XmlReader reader)
		{
			var events = new Dictionary<string, Event>();
			var fields = new Dictionary<string, Field>();
			var methods = new Dictionary<string, Method>();
			var properties = new Dictionary<string, Property>();
			var types = new Dictionary<string, Type>();

			while (await reader.ReadAsync())
			{
				if (reader is not { NodeType: XmlNodeType.Element, Name: "member" })
				{
					continue;
				}

				if (reader.GetAttribute("name") is not string name || !MemberIdentifier.TryParse(name, out var identifier))
				{
					continue;
				}

				switch (await Member.ReadAsync(reader, identifier))
				{
					case Event e:
						events.Add(identifier.FullName, e);
						break;
					case Field f:
						fields.Add(identifier.FullName, f);
						break;
					case Method m:
						methods.Add(identifier.FullName, m);
						break;
					case Property p:
						properties.Add(identifier.FullName, p);
						break;
					case Type t:
						types.Add(identifier.FullName, t);
						break;
				}
			}

			return new Documentation(
				events,
				fields,
				methods,
				properties,
				types);
		}
	}

	public IReadOnlyDictionary<string, Event> Events { get; }
	public IReadOnlyDictionary<string, Field> Fields { get; }
	public IReadOnlyDictionary<string, Method> Methods { get; }
	public IReadOnlyDictionary<string, Property> Properties { get; }
	public IReadOnlyDictionary<string, Type> Types { get; }

	private Documentation(
		IReadOnlyDictionary<string, Event> events,
		IReadOnlyDictionary<string, Field> fields,
		IReadOnlyDictionary<string, Method> methods,
		IReadOnlyDictionary<string, Property> properties,
		IReadOnlyDictionary<string, Type> types)
	{
		Events = events;
		Fields = fields;
		Methods = methods;
		Properties = properties;
		Types = types;
	}
}
