namespace vintage_basic.Language.VintageBasic;
sealed class BasicExecuter
{
	public void ExecuteFile(string fileName)
	{
		string text = File.ReadAllText(fileName);
		Execute(fileName, text);
	}

	/// <summary>
	///  Executes BASIC code from a string. The file path is provided only for error reporting.
	/// </summary>
	public void Execute(string fileName, string text)
	{
		var rawLines = ScanLines(fileName, text);
		var tokenizedLines = rawLines.Select(TokenizeLine).ToList();
		//List<ParsedLine> parsedLines = tokenizedLines.Select(ParseLine).ToList();

		//RunProgram(parsedLines);
	}

	/// <summary>
	/// Transforms the BASIC source into a series of 'RawLine's using the 'rawLinesP' LineScanner.
	/// </summary>
	static IEnumerable<RawLine> ScanLines(string fileName, string text)
	{	
		return LineScanner.ParseRawLines(text);
	}

	/// <summary>
	/// Tokenizes a 'RawLine' into a 'TokenizedLine' using the 'taggedTokensP' Tokenizer.
	/// </summary>
	private List<Tagged<Token>> TokenizeLine(RawLine rawLine)
	{
		return Tokenizer.Tokenize(rawLine.Content);
	}

	///// <summary>
	///// Parses a 'TokenizedLine' to yield a 'Line', using the 'statementListP' Parser.
	///// </summary>
	//private ParsedLine ParseLine(List<TaggedToken> tokenizedLine)
	//{
	//	var parser = new BasicParser(tokenizedLine);
	//	parser.ParseStatement();
	//	return null;
	//	//return BasicParser.ParseStatement(tokenizedLine);
	//}

	//private void RunProgram(List<ParsedLine> parsedLines)
	//{
	//	Interpreter.Interpret(parsedLines);
	//}
}