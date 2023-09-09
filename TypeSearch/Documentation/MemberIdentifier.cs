using System.Diagnostics.CodeAnalysis;

namespace TypeSearch.Documentation;

public class MemberIdentifier : ISpanParsable<MemberIdentifier>
{
	public static MemberIdentifier Parse(string s) =>
		Parse(s, null);

	public static MemberIdentifier Parse(string s, IFormatProvider? provider) =>
		Parse(s.AsSpan(), provider);

	public static bool TryParse(string? s, [NotNullWhen(true)] out MemberIdentifier? result) =>
		TryParse(s, null, out result);

	public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out MemberIdentifier? result) =>
		TryParse(s.AsSpan(), provider, out result);

	public static MemberIdentifier Parse(ReadOnlySpan<char> s) =>
		Parse(s, null);

	public static MemberIdentifier Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		TryParse(s, provider, out var result)
			? result
			: throw new FormatException();

	public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out MemberIdentifier? result) =>
		TryParse(s, null, out result);

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out MemberIdentifier? result)
	{
		result = null;

		if (s is not [var c, ':', .. var rest])
		{
			return false;
		}

		if (!TryParseMemberKind(c, out var kind))
		{
			return false;
		}

		result = new MemberIdentifier(kind.Value, rest.ToString());
		return true;

		static bool TryParseMemberKind(char c, [NotNullWhen(true)] out MemberKind? kind)
		{
			switch (c)
			{
				case 'E':
					kind = MemberKind.Event;
					return true;
				case 'F':
					kind = MemberKind.Field;
					return true;
				case 'M':
					kind = MemberKind.Method;
					return true;
				case 'P':
					kind = MemberKind.Property;
					return true;
				case 'T':
					kind = MemberKind.Type;
					return true;
				default:
					kind = null;
					return false;
			}
		}
	}

	public MemberKind Kind { get; }
	public string FullName { get; }

	public MemberIdentifier(MemberKind kind, string fullName)
	{
		Kind = kind;
		FullName = fullName;
	}
}
