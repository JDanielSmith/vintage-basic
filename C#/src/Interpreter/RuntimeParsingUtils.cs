using System.Globalization;
using System.Text; // For StringBuilder

namespace VintageBasic.Interpreter;

static class RuntimeParsingUtils
{
	public static bool TryParseFloat(string s, out float value)
	{
		return Single.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
	}

	public static IEnumerable<string> ParseDataLineContent(string rawContent)
	{
		var current = 0;
		StringBuilder builder = new();
		bool inQuotes = false;
		while (current < rawContent.Length)
		{
			char c = rawContent[current];

			if (inQuotes)
			{
				if (c == '"')
				{
					if (current + 1 < rawContent.Length && rawContent[current + 1] == '"') // Escaped quote ""
					{
						builder.Append('"');
						current++;
					}
					else // End of quoted string
					{
						inQuotes = false;
						// The value is in builder. It will be added upon comma or EOL.
					}
				}
				else
				{
					builder.Append(c);
				}
			}
			else // Not in quotes
			{
				if (c == '"')
				{
					inQuotes = true;
					// If builder has content, it implies the previous item was unquoted and now finished.
					// This typically shouldn't happen if quotes are only at the start of an item.
					// e.g. DATA 123"string" would be problematic.
					// Assuming quotes mark the beginning of an item if builder is empty.
					if (builder.Length > 0)
					{
						// This case is ambiguous in many BASICs. Let's assume it's an error or part of an unquoted string.
						// For simplicity, we'll treat it as part of the current unquoted item.
						builder.Append(c);
					}
				}
				else if (c == ',')
				{
					yield return builder.ToString().Trim(); // Trim unquoted items
					builder.Clear();
				}
				else
				{
					builder.Append(c);
				}
			}
			current++;
		}

		// If inQuotes is true here, it's an unterminated string.
		// BASIC might error or treat it as if the quote was literal.
		// We'll add the content as parsed, including the opening quote if it was unterminated.
		//
		// This means an unterminated string. Add what's in builder.
		// The parser for string literals in tokenizer handles unterminated strings by taking what's there.
		// Here, we are parsing the *content* of a DATA statement.
		// For `DATA "abc`, builder contains `abc`. For `DATA "abc""def`, builder contains `abc"def`.
		var retval = builder.ToString();
		yield return inQuotes ? retval : retval.Trim();
	}
}
