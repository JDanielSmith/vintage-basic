using System.Globalization;
using System.Text; // For StringBuilder
using VintageBasic.Runtime;
using VintageBasic.Syntax;

namespace VintageBasic.Interpreter;

static class RuntimeParsingUtils
{
    public static bool TryParseFloat(string s, out float value)
    {
        return float.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
    
    public static List<string> ParseDataLineContent(string rawContent)
    {
        List<string> values = [];
        if (rawContent is null) return values;

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
                    values.Add(builder.ToString().Trim()); // Trim unquoted items
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }
            current++;
        }

        // Add the last item
        // If inQuotes is true here, it's an unterminated string.
        // BASIC might error or treat it as if the quote was literal.
        // We'll add the content as parsed, including the opening quote if it was unterminated.
        if (inQuotes) {
             // This means an unterminated string. Add what's in builder.
             // The parser for string literals in tokenizer handles unterminated strings by taking what's there.
             // Here, we are parsing the *content* of a DATA statement.
             // For `DATA "abc`, builder contains `abc`. For `DATA "abc""def`, builder contains `abc"def`.
             values.Add(builder.ToString()); // Do not trim quoted strings
        } else {
             values.Add(builder.ToString().Trim()); // Trim unquoted items
        }
        
        // If rawContent was empty, values will be empty.
        // If rawContent was "   ", values will contain one "" (empty string).
        // If rawContent was ",", values will contain two "" (empty strings).
        // If rawContent was " , ", values will be ["",""].
        // If rawContent was "\"\"", values will be [""] (empty string, not an empty list).
        // If rawContent was "\" \"", values will be [" "] (string with a space).

        return values;
    }


    public static string PrintFloat(float f)
    {
        string s;
        if (f == 0f && BitConverter.SingleToUInt32Bits(f) == 0x80000000) 
        {
             s = "-0";
        }
        else if (Math.Abs(f) >= 1e-4 && Math.Abs(f) < 1e7 || f == 0.0)
        {
            s = f.ToString("0.#######", CultureInfo.InvariantCulture);
             if (s.Contains('.'))
            {
                s = s.TrimEnd('0').TrimEnd('.');
            }
        }
        else
        {
            s = f.ToString("0.######E+00", CultureInfo.InvariantCulture);
        }
        return (f >= 0 && s[0] != '-' ? " " : "") + s + " ";
    }
    
    public static string Trim(string s)
    {
        return s.Trim();
    }

    public static Val? CheckInput(VarName targetVarName, string inputString)
    {
        string stringToParse = (targetVarName.Type == ValType.StringType) ? inputString : inputString.Trim();

        switch (targetVarName.Type)
        {
            case ValType.StringType:
                return new StringVal(stringToParse); 
            case ValType.FloatType:
                if (TryParseFloat(stringToParse, out float fv)) 
                {
                    return new FloatVal(fv);
                }
                return null; 
            case ValType.IntType:
                if (TryParseFloat(stringToParse, out float fvForInt)) 
                {
                    return new IntVal(RuntimeContext.FloatToInt(fvForInt));
                }
                return null; 
            default:
                return null; 
        }
    }
}
