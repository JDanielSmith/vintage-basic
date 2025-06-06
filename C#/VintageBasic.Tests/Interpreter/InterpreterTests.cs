// VintageBasic.Tests/Interpreter/InterpreterTests.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using VintageBasic.Parsing;
using VintageBasic.Syntax;
using VintageBasic.Runtime;
using VintageBasic.Interpreter;
using VintageBasic.Tests.IO; // For StringInputStream, StringBuilderOutputStream
using Xunit;

namespace VintageBasic.Tests.Interpreter
{
    public class InterpreterTests
    {
        private (StringBuilderOutputStream outputStream, RuntimeContext context) ExecuteBasicProgram(string programText, string consoleInput = "")
        {
            var parsedLines = Parser.ParseProgram(programText);

            var inputStream = StringInputStream.FromStringWithNewlines(consoleInput);
            var outputStream = new StringBuilderOutputStream();

            var store = new BasicStore();
            // Interpreter now handles loading DATA statements via IOManager from parsed lines
            var state = new BasicState(inputStream, outputStream, new List<string>()); 
            var context = new RuntimeContext(store, state);
            var interpreter = new VintageBasic.Interpreter.Interpreter(context);

            interpreter.ExecuteProgram(parsedLines);
            return (outputStream, context);
        }

        [Fact]
        public void TestPrintStatementOutput()
        {
            // Arrange
            string programText = "10 PRINT \"HELLO, WORLD!\"";
            
            // Act
            var (outputStream, _) = ExecuteBasicProgram(programText);
            string output = outputStream.GetOutput();

            // Assert
            // PRINT adds a newline. PrintVal for strings doesn't add extra spaces.
            Assert.Equal("HELLO, WORLD!\n", output);
        }

        [Fact]
        public void TestLetStatementAndVariableState()
        {
            // Arrange
            string programText = "10 LET A = 123\n20 LET B$ = \"TEST\"\n30 LET C% = A + 7";
            
            // Act
            var (_, context) = ExecuteBasicProgram(programText);

            // Assert
            var varA = new VarName(ValType.FloatType, "A");
            var varB_Str = new VarName(ValType.StringType, "B"); // B$
            var varC_Int = new VarName(ValType.IntType, "C");   // C%
            
            Val valA = context.Variables.GetScalarVar(varA);
            Assert.IsType<FloatVal>(valA);
            Assert.Equal(123.0f, ((FloatVal)valA).Value);

            Val valB_Str = context.Variables.GetScalarVar(varB_Str);
            Assert.IsType<StringVal>(valB_Str);
            Assert.Equal("TEST", ((StringVal)valB_Str).Value);
            
            Val valC_Int = context.Variables.GetScalarVar(varC_Int);
            // LET C% = A + 7. A is 123. 123+7 = 130. Coerced to IntVal.
            Assert.IsType<IntVal>(valC_Int); 
            Assert.Equal(130, ((IntVal)valC_Int).Value);
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

            var varN_Str = new VarName(ValType.StringType, "N");
            Val valN_Str = context.Variables.GetScalarVar(varN_Str);
            Assert.IsType<StringVal>(valN_Str);
            Assert.Equal("CHATBOT", ((StringVal)valN_Str).Value);
        }
        
        [Fact]
        public void TestSimpleForLoop()
        {
            string programText = "10 FOR I = 1 TO 3\n20 PRINT I\n30 NEXT I";
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
			string programText = "10 LINES=17\r\n20 FORI=1TOLINES/2+1\r\n30 FORJ=1TO(LINES+1)/2-I+1:PRINT\" \";:NEXT\r\n40 FORJ=1TOI*2-1:PRINT\"*\";:NEXT\r\n50 PRINT\r\n60 NEXTI\r\n70 FORI=1TOLIVES/2:REM note misspelled variable is the same\r\n75 REM because variables are unique to only two characters\r\n80 FORJ=1TOI+1:PRINT\" \";:NEXT\r\n90 FORJ=1TO((LINES+1)/2-I)*2-1:PRINT\"*\";:NEXT\r\n100 PRINT\r\n110 NEXTI";

			var (outputStream, _) = ExecuteBasicProgram(programText);

			string expectedOutput = "         ***\n        *****\n       *******\n      *********\n     ***********\n    *************\n   ***************\n  *****************\n *******************\n  *****************\n   ***************\n    *************\n     ***********\n      *********\n       *******\n        *****\n         ***\n";
			Assert.Equal(expectedOutput, outputStream.GetOutput());
		}
	}
}
