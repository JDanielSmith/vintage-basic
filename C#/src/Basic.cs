// src/Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VintageBasic.IO;
using VintageBasic.Parsing;
using VintageBasic.Parsing.Errors;
using VintageBasic.Syntax;
using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;
using VintageBasic.Interpreter;

public class Basic
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} <SOURCE_FILE.bas>");
            Environment.ExitCode = 1;
            return;
        }

        string filePath = args[0];
        string fileContent;

        try
        {
            fileContent = File.ReadAllText(filePath);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: File not found '{filePath}'.");
            Environment.ExitCode = 2;
            return;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error reading file '{filePath}': {ex.Message}");
            Environment.ExitCode = 3;
            return;
        }

        List<Line> parsedLines;
        try
        {
            parsedLines = Parser.ParseProgram(fileContent);
        }
        catch (ParseException ex)
        {
            Console.Error.WriteLine(ex.Message); // ParseException should have good formatting
            Environment.ExitCode = 4;
            return;
        }
        catch (Exception ex) // Catch other unexpected parsing-related errors
        {
            Console.Error.WriteLine($"Unexpected parsing error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.ExitCode = 5;
            return;
        }
        
        // If parsing results in no executable lines (e.g. only comments or empty numbered lines)
        // it's not necessarily an error, but the program won't do anything.
        if (!parsedLines.Any(line => line.Statements.Any()))
        {
            // Optionally print a message or just exit cleanly.
            // Console.WriteLine("Program contains no executable statements.");
            Environment.ExitCode = 0; // Success, but did nothing.
            return;
        }


        var consoleInputStream = new ConsoleInputStream();
        var consoleOutputStream = new ConsoleOutputStream();

        var store = new BasicStore();
        // Pass an empty list for initialDataStatements; Interpreter will load from parsed DATA statements.
        var state = new BasicState(consoleInputStream, consoleOutputStream, new List<string>()); 
        var context = new RuntimeContext(store, state);
        var interpreter = new VintageBasic.Interpreter.Interpreter(context);

        try
        {
            interpreter.ExecuteProgram(parsedLines);
            Environment.ExitCode = 0; // Success
        }
        catch (BasicRuntimeException ex)
        {
            // BasicRuntimeException messages are already formatted with line numbers.
            Console.Error.WriteLine($"Runtime error: {ex.Message}");
            Environment.ExitCode = 10; // Runtime error code
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected runtime error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.ExitCode = 11; // Unexpected runtime error code
        }
    }
}
