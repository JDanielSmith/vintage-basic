// src/VintageBasic/Runtime/BasicState.cs
using System;
using System.Collections.Generic;

namespace VintageBasic.Runtime;

sealed class BasicState
{
    public IInputStream InputStream { get; set; }
    public IOutputStream OutputStream { get; set; }
    
    public int CurrentLineNumber { get; set; } 
    public int OutputColumn { get; set; } 
    public double PreviousRandomValue { get; set; } 
    public Random RandomGenerator { get; set; }
    
    // Stores all strings from DATA statements in the program, in order of appearance.
    private readonly IReadOnlyList<string> allDataStatements;
    
    // Pointer to the current DATA statement item to be read.
    public int DataReadPointer { get; private set; }

    public Stack<int> GosubReturnStack { get; }
    public Stack<Interpreter.ForLoopContext> ForLoopStack { get; }


    public BasicState(IInputStream inputStream, IOutputStream outputStream, IReadOnlyList<string> initialDataStatements, int initialSeed = 0)
    {
        InputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        OutputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        CurrentLineNumber = 0; 
        OutputColumn = 0;
        RandomGenerator = (initialSeed == 0) ? new Random() : new Random(initialSeed);
        PreviousRandomValue = RandomGenerator.NextDouble(); 
        allDataStatements = new List<string>(initialDataStatements ?? new List<string>()); // Store a copy
        DataReadPointer = 0;
        GosubReturnStack = new Stack<int>();
        ForLoopStack = new Stack<Interpreter.ForLoopContext>();
    }

    // Gets the next available data String. Returns null if no more data is available.
    public string? ReadNextData()
    {
        if (DataReadPointer < allDataStatements.Count)
        {
            return allDataStatements[DataReadPointer++];
        }
        return null;
    }

    // Resets the data pointer to the beginning of the data statements.
    public void RestoreDataPointer()
    {
        DataReadPointer = 0;
    }

    // Resets the data pointer to a specific (not yet implemented, needs program structure access)
    // For now, this will also reset to the beginning.
    // To implement RESTORE <line_number>, we'd need to map line numbers to indices in allDataStatements
    // or rebuild allDataStatements from a specific line.
    public void RestoreDataPointer(int? targetLineLabel)
    {
        // Simplified: for now, any RESTORE resets to the beginning.
        // Full implementation would require knowing which DATA items belong to which line.
        DataReadPointer = 0;
    }

    // Method to re-initialize data statements after program load.
    // This is a bit of a workaround for the current structure.
    // Ideally, BasicState is constructed with all data from the start.
    public static void InitializeDataStatements(IReadOnlyList<string> newDataStatements)
    {
        // This replaces the internal list. Be cautious if BasicState is shared and re-initialized mid-execution.
        // For initial setup, this should be fine.
        // The original 'allDataStatements' was readonly after constructor.
        // To make this work, we need to remove readonly or use a different mechanism.
        // For now, let's assume we can assign to a private field.
        // This requires changing `allDataStatements` to not be readonly.
        // Or, better, `IOManager` could manage the data list based on `BasicState` and `JumpTable`.
        // For now, let's stick to modifying BasicState for simplicity of this step.
        // This means `allDataStatements` field should not be `readonly`.
        // I will make this change when I edit `BasicState.cs` fully.
        // For now, this is a conceptual addition.

        // Re-evaluating: The constructor already takes initialDataStatements.
        // The Interpreter should collect DATA first, then the BasicState should be created.
        // If we must modify after construction, then `allDataStatements` cannot be readonly.
        // Let's assume for this task, we will modify Interpreter to pass data to IOManager,
        // and IOManager will use it to feed ReadData, and handle RestoreData.
        // This avoids changing BasicState's readonly nature post-construction for data.
        // The Haskell `setDataStrings` is a monad action.
        // So, _ioManager.SetDataStrings(allDataStrings) seems like a better approach.
        // This means InputOutputManager needs a new method.
    }
}
