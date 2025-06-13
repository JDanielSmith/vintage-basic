using VintageBasic.Parsing;
using VintageBasic.Runtime;
using VintageBasic.Syntax;
using VintageBasic.Tests.IO; // For StringInputStream, StringBuilderOutputStream
using Xunit;

namespace VintageBasic.Tests.Interpreter;

public class InterpreterTests
{
	static (StringBuilderOutputStream outputStream, RuntimeContext context) ExecuteBasicProgram(string programText, string consoleInput = "")
	{
		var parsedLines = Parser.ParseProgram(programText);

		var inputStream = StringInputStream.FromStringWithNewlines(consoleInput);
		StringBuilderOutputStream outputStream = new();

		BasicStore store = new();
		// Interpreter now handles loading DATA statements via IOManager from parsed lines
		BasicState state = new(inputStream, outputStream);
		RuntimeContext context = new(store, state);
		VintageBasic.Interpreter.Interpreter interpreter = new(context);

		interpreter.ExecuteProgram(parsedLines);
		return (outputStream, context);
	}

	[Fact]
	public void TestPrintStatementOutput()
	{
		string programText = "10 print \"HELLO, WORLD!\"";

		var (outputStream, _) = ExecuteBasicProgram(programText);
		string output = outputStream.GetOutput();

		// PRINT adds a newline. PrintVal for strings doesn't add extra spaces.
		Assert.Equal("HELLO, WORLD!\n", output);
	}

	[Fact]
	public void TestLetStatementAndVariableState()
	{
		string programText = "10 let a = 123\n20 let b$ = \"TEST\"\n30 let c% = A + 7";

		var (_, context) = ExecuteBasicProgram(programText);

		var varA = VarName.CreateFloat("A");
		var varB_Str = VarName.CreateString("B"); // B$
		var varC_Int = VarName.CreateInt("C");   // C%

		Object valA = context.Variables.GetScalarVar(varA);
		Assert.IsType<float>(valA);
		Assert.Equal(123.0f, (float)valA);

		Object valB_Str = context.Variables.GetScalarVar(varB_Str);
		Assert.IsType<string>(valB_Str);
		Assert.Equal("TEST", (string)valB_Str);

		Object valC_Int = context.Variables.GetScalarVar(varC_Int);
		// LET C% = A + 7. A is 123. 123+7 = 130. Coerced to Int32.
		Assert.IsType<int>(valC_Int);
		Assert.Equal(130, (int)valC_Int);
	}

	[Fact]
	public void TestInputStatement()
	{
		string programText = "10 INPUT \"WHAT IS YOUR NAME?\"; N$\n20 PRINT \"HELLO, \"; N$";
		string consoleInput = "CHATBOT\n"; // Simulate user typing "CHATBOT" and pressing Enter.

		var (outputStream, context) = ExecuteBasicProgram(programText, consoleInput);
		string output = outputStream.GetOutput();

		// Expected output: Prompt, then "? ", then the final PRINT output.
		// The "? " comes from the Interpreter's INPUT handling.
		Assert.Equal("WHAT IS YOUR NAME?? HELLO, CHATBOT\n", output);

		var varN_Str = VarName.CreateString("N");
		Object valN_Str = context.Variables.GetScalarVar(varN_Str);
		Assert.IsType<string>(valN_Str);
		Assert.Equal("CHATBOT", (string)valN_Str);
	}

	[Fact]
	public void SimpleForLoopTest()
	{
		const string programText = """
			10 FOR I = 1 TO 3
			20 PRINT I
			30 NEXT I
			""";

		var (outputStream, _) = ExecuteBasicProgram(programText);

		string expectedOutput = " 1 \n 2 \n 3 \n"; // PrintVal adds leading/trailing spaces for numbers
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void MathTest()
	{
		string programText = "10 INPUT\"ENTER A NUMBER\";N\r\n20 ?\"ABS(N)=\";ABS(N)\r\n25 ?\"ATN(N)=\";ATN(N)\r\n30 ?\"COS(N)=\";COS(N)\r\n40 ?\"EXP(N)=\";EXP(N)\r\n50 ?\"INT(N)=\";INT(N)\r\n60 ?\"LOG(N)=\";LOG(N)\r\n70 ?\"SGN(N)=\";SGN(N)\r\n80 ?\"SQR(N)=\";SQR(N)\r\n90 ?\"TAN(N)=\";TAN(N)";
		string consoleInput = "16.0\n";

		var (outputStream, _) = ExecuteBasicProgram(programText, consoleInput);

		string expectedOutput = "ENTER A NUMBER? ABS(N)= 16 \nATN(N)= 1.508378 \nCOS(N)=-0.9576595 \nEXP(N)= 8886111 \nINT(N)= 16 \nLOG(N)= 2.772589 \nSGN(N)= 1 \nSQR(N)= 4 \nTAN(N)= 0.3006322 \n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void NameTest()
	{
		string programText = "10 INPUT\"WHAT IS YOUR NAME\";NAME$\r\n20 INPUT\"ENTER A NUMBER\";N\r\n30 FORI=1TON\r\n40 ?\"HELLO, \";NAME$;\"!\"\r\n50 NEXT";
		string consoleInput = "Dan\n2\n";

		var (outputStream, _) = ExecuteBasicProgram(programText, consoleInput);

		string expectedOutput = "WHAT IS YOUR NAME? ENTER A NUMBER? HELLO, Dan!\nHELLO, Dan!\n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void StringsTest()
	{
		string programText = "10 INPUT\"ENTER A STRING\";A$\r\n20 INPUT\"ENTER A NUMBER\";N\r\n30 ?\"ASC(A$)=\";ASC(A$)\r\n40 ?\"CHR$(N)=\";CHR$(N)\r\n50 ?\"LEFT$(A$,N)=\";LEFT$(A$,N)\r\n60 ?\"MID$(A$,N)=\";MID$(A$,N)\r\n70 ?\"MID$(A$,N,3)=\";MID$(A$,N,3)\r\n80 ?\"RIGHT$(A$,N)=\";RIGHT$(A$,N)\r\n90 ?\"LEN(A$)=\";LEN(A$)\r\n100 ?\"VAL(A$)=\";VAL(A$)\r\n110 ?\"STR$(N)=\";STR$(N)\r\n120 ?\"SPC(N)='\";SPC(N);\"'\"";
		string consoleInput = "abcdef\n2\n";

		var (outputStream, _) = ExecuteBasicProgram(programText, consoleInput);

		string expectedOutput = "ENTER A STRING? ENTER A NUMBER? ASC(A$)= 97 \nCHR$(N)=\u0002\nLEFT$(A$,N)=ab\nMID$(A$,N)=bcdef\nMID$(A$,N,3)=bcd\nRIGHT$(A$,N)=ef\nLEN(A$)= 6 \nVAL(A$)= 0 \nSTR$(N)= 2\nSPC(N)='  '\n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void StarsTest()
	{
		string programText = "10 INPUT \"What is your name\"; U$\r\n20 PRINT \"Hello \"; U$\r\n30 INPUT \"How many stars do you want\"; N\r\n40 S$ = \"\"\r\n50 FOR I = 1 TO N\r\n60 S$ = S$ + \"*\"\r\n70 NEXT I\r\n80 PRINT S$\r\n90 INPUT \"Do you want more stars\"; A$\r\n100 IF LEN(A$) = 0 THEN 90\r\n110 A$ = LEFT$(A$, 1)\r\n120 IF A$ = \"Y\" OR A$ = \"y\" THEN 30\r\n130 PRINT \"Goodbye \";U$\r\n140 END";
		string consoleInput = "Dan\n10\nyes\n5\nNo\n";

		var (outputStream, _) = ExecuteBasicProgram(programText, consoleInput);

		string expectedOutput = "What is your name? Hello Dan\nHow many stars do you want? **********\nDo you want more stars? How many stars do you want? *****\nDo you want more stars? Goodbye Dan\n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void DiamondTest()
	{
		const string programText = """
			10 LINES=17
			20 FORI=1TOLINES/2+1
			30 FORJ=1TO(LINES+1)/2-I+1:PRINT" ";:NEXT
			40 FORJ=1TOI*2-1:PRINT"*";:NEXT
			50 PRINT
			60 NEXTI
			70 FORI=1TOLIVES/2:REM note misspelled variable is the same
			75 REM because variables are unique to only two characters
			80 FORJ=1TOI+1:PRINT" ";:NEXT
			90 FORJ=1TO((LINES+1)/2-I)*2-1:PRINT"*";:NEXT
			100 PRINT
			110 NEXTI			
		""";

		var (outputStream, _) = ExecuteBasicProgram(programText);

		string expectedOutput = "         ***\n        *****\n       *******\n      *********\n     ***********\n    *************\n   ***************\n  *****************\n *******************\n  *****************\n   ***************\n    *************\n     ***********\n      *********\n       *******\n        *****\n         ***\n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}

	[Fact]
	public void RESTORE_Test()
	{
		const string programText = """
			10 DATA 5, 10, 15, 20 : REM this is our data
			20 READ A, B
			30 PRINT A, B  : REM Output: 5 10
			40 RESTORE
			50 READ A, B
			60 PRINT A, B  : REM Output: 5 10 (RESTORE resets the pointer)
			""";

		var (outputStream, _) = ExecuteBasicProgram(programText);

		string expectedOutput = " 5             10 \n 5             10 \n";
		Assert.Equal(expectedOutput, outputStream.GetOutput());
	}
}
