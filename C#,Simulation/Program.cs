/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
*/

//This code only has single order execution
public class Instruction
{
    public string Dest { get; }
    public string LeftOperand { get; }
    public string Operator { get; }
    public string RightOperand { get; }
    public int CycleCost { get; }

    private static readonly Dictionary<string, int> CycleCostEnum = new Dictionary<string, int>
    {
        { "+", 1 },
        { "-", 1 },
        { "*", 2 },
        { "Store", 3 },
        { "Load", 3 }
    };

    public Instruction(string dest, string leftOperand, string op, string rightOperand, int cycleCost)
    {
        Dest = dest;
        LeftOperand = leftOperand;
        Operator = op;
        RightOperand = rightOperand;
        CycleCost = cycleCost;
    }

    public static Instruction Parse(string line)
    {
        var parts = line.Split(" = ", StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid instruction format: {line}");

        var dest = parts[0];
        var expression = parts[1];

        if (expression == "Store" || expression == "Load")
        {
            return new Instruction(dest, null, expression, null, CycleCostEnum[expression]);
        }

        var exprParts = expression.Split(' ');
        if (exprParts.Length != 3)
            throw new ArgumentException($"Invalid arithmetic instruction format: {line}");

        var leftOperand = exprParts[0];
        var op = exprParts[1];
        var rightOperand = exprParts[2];

        if (!CycleCostEnum.ContainsKey(op))
            throw new ArgumentException($"Invalid operator: {op}");

        return new Instruction(dest, leftOperand, op, rightOperand, CycleCostEnum[op]);
    }

    public override string ToString()
    {
        return Operator == "Store" || Operator == "Load"
            ? $"{Dest} = {Operator}"
            : $"{Dest} = {LeftOperand} {Operator} {RightOperand}";
    }
}

public class Scheduler
{

    private Dictionary<string, int> RegsRead = new Dictionary<string, int>();

    private Dictionary<string, int> RegsWritten = new Dictionary<string, int>();

    public Scheduler()
    {

        SetUpDictionaries();

    }

    private readonly Dictionary<string, int> _busyUntil = new();


    public bool IsReady(Instruction instruction)
    {

        if (CheckForDependecies(instruction) == 0)
        {

            UpdateDictionaries(instruction);

            return true;
        }

        return false;
    }

    public void RetireInstruction(Instruction instruction)
    {

        RegsWritten[instruction.Dest] = -1;
        if (instruction.Operator != "Store" && instruction.Operator != "Load")
        {
            RegsRead[instruction.LeftOperand] = -1;
            RegsRead[instruction.RightOperand] = -1;
        }
    }
    /*
    public void Reserve(Instruction instruction, int currentCycle)
    {
        _busyUntil[instruction.Dest] = currentCycle + instruction.CycleCost;
    }
    */
    private int CheckForDependecies(Instruction instruction)
    {
        //Check for ReadAfterWrite

        //Would check the Regs to make sure the operands are not being written to
        //ReadAfters cannot happen if the instruction is a Store or a Load
        if (instruction.Operator != "Store" && instruction.Operator != "Load")
        {
            //Console.WriteLine("-The operation is not Store or Load, meaning it will try and find a ReadAfterWrite-");
            if (RegsWritten[instruction.LeftOperand] == 1 || RegsWritten[instruction.RightOperand] == 1)
            {

                return 1;


            }
        }
        //Checks the condition for WriteAfterRead
        if (RegsRead[instruction.Dest] != 0)
        {


            return 2;
        }
        //Checks the condition for Write after Write
        else if (RegsWritten[instruction.Dest] != 0)
        {


            return 3;
        }

        //If it reached the true, it means this has no dependecies
        return 0;

    }

    private void UpdateDictionaries(Instruction instruction)
    {

        RegsWritten[instruction.Dest] = +1;
        RegsRead[instruction.LeftOperand] = +1;
        RegsRead[instruction.RightOperand] = +1;

    }

    private void SetUpDictionaries()
    {

        RegsRead.Add("R0", 0);
        RegsRead.Add("R1", 0);
        RegsRead.Add("R2", 0);
        RegsRead.Add("R3", 0);
        RegsRead.Add("R4", 0);
        RegsRead.Add("R5", 0);
        RegsRead.Add("R6", 0);
        RegsRead.Add("R7", 0);

        RegsWritten.Add("R0", 0);
        RegsWritten.Add("R1", 0);
        RegsWritten.Add("R2", 0);
        RegsWritten.Add("R3", 0);
        RegsWritten.Add("R4", 0);
        RegsWritten.Add("R5", 0);
        RegsWritten.Add("R6", 0);
        RegsWritten.Add("R7", 0);
    }
}

public class Processor
{
    private readonly List<Instruction> _instructions;
    private readonly Scheduler _scheduler = new();
    private int _currentCycle;
    private HashSet<int> _retired = new();
    //private Instruction _inFlight;
    //private bool _waitForRetire;

    private int _issue_slots;

    public Processor(List<Instruction> instructions, int Configuration, int issue_slots)
    {
        _instructions = instructions;
        _issue_slots = issue_slots;

        // Helps me run the correct configuration depending on what was asked when creating the class
        if (Configuration == 1)
        {
            Single_Instruction_run();

        }
        else if (Configuration == 2)
        {

            Superscalar_in_order_run();

        }
        else if (Configuration == 3)
        {

            Superscalar_out_of_order_run();

        }

    }

    public void Single_Instruction_run()
    {
        int instructionIndex = 0;
        int totalInstructions = _instructions.Count;
        Instruction currentInstruction = null;
        int retireCycle = 0;
        bool waitForNextCycle = false; // Flag to track waiting after retirement

        Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
        Console.WriteLine(new string('-', 60));

        while (instructionIndex < totalInstructions || currentInstruction != null)
        {
            _currentCycle++;
            string issuedInstruction = "";
            string retiredInstruction = "";

            // Retire the current instruction if its cycle cost is completed
            if (currentInstruction != null && _currentCycle == retireCycle)
            {
                int retiredIndex = _instructions.IndexOf(currentInstruction) + 1;
                retiredInstruction = $"Instruction {retiredIndex}";
                currentInstruction = null;
                waitForNextCycle = true; // Set the flag to wait for the next cycle
            }

            // Issue a new instruction only if:
            // 1. There is no current instruction in-flight.
            // 2. We're not waiting for the next cycle after a retirement.
            if (currentInstruction == null && !waitForNextCycle && instructionIndex < totalInstructions)
            {
                currentInstruction = _instructions[instructionIndex];
                issuedInstruction = currentInstruction.ToString();
                retireCycle = _currentCycle + currentInstruction.CycleCost;
                instructionIndex++;
            }

            // Reset the wait flag if it was set during the previous cycle
            if (waitForNextCycle && currentInstruction == null)
            {
                waitForNextCycle = false;
            }

            Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
    }

    private void Superscalar_in_order_run()
    {
        int instructionIndex = 0;
        int totalInstructions = _instructions.Count;
        var currentInstructions = new Instruction[_issue_slots];
        var retireCycles = new int[_issue_slots];
        bool[] stalled = new bool[_issue_slots];

        Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
        Console.WriteLine(new string('-', 60));

        // Run until all instructions are retired
        while (instructionIndex < totalInstructions || currentInstructions.Any(ci => ci != null))
        {
            _currentCycle++;
            string issuedInstruction = "";
            string retiredInstruction = "";

            // Retire instructions that have completed
            for (int i = 0; i < _issue_slots; i++)
            {
                if (currentInstructions[i] != null && _currentCycle == retireCycles[i])
                {
                    int retiredIndex = _instructions.IndexOf(currentInstructions[i]) + 1;
                    _scheduler.RetireInstruction(currentInstructions[i]); // Free registers after retiring
                    retiredInstruction += $"Instruction {retiredIndex} ";
                    currentInstructions[i] = null;
                    stalled[i] = true; // Stall the slot for the next cycle
                }
            }

            // Reset stall flags for slots after a cycle
            for (int i = 0; i < _issue_slots; i++)
            {
                if (stalled[i] && currentInstructions[i] == null)
                {
                    stalled[i] = false;
                }
            }

            // Issue instructions if slots are free and not stalled
            for (int i = 0; i < _issue_slots; i++)
            {
                if (currentInstructions[i] == null && !stalled[i] && instructionIndex < totalInstructions)
                {
                    var instruction = _instructions[instructionIndex];

                    // Check dependencies before issuing
                    if (_scheduler.IsReady(instruction))
                    {
                        currentInstructions[i] = instruction;
                        issuedInstruction += instruction.ToString() + " ";
                        retireCycles[i] = _currentCycle + instruction.CycleCost;
                        instructionIndex++;
                    }
                    else
                    {
                        // Dependency detected, continue progressing cycles
                        Console.WriteLine($"Cycle {_currentCycle}: Dependency detected for {instruction}");
                    }
                }
            }

            // Print the cycle summary
            Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
    }



    private void Superscalar_out_of_order_run()
    {

    }
}

public static class Program
{
    public static void Main()
    {

        //Needs to make sure the file path of the instructions.txt is the correct one, this cannot be shortened 

        var instructions = LoadInstructions("C:\\Users\\jairo\\OneDrive\\Documentos\\GitHub\\Computer-Architecture-Project\\instructions.txt");
        //Small test that makes sure the instruction are read in the correct way
        /*
        foreach (var instruction in instructions)
        {
            // Print the class name of each instruction
            Console.WriteLine(instruction);
        }
        */

        //Runs the processor with sinlge instruction, in order, with one issue slot
        var processor = new Processor(instructions, 1, 1);
        var processorInOrder = new Processor(instructions, 2, 1);
        //var processorOutofOrder = new Processor(instructions, 3, 1);
        //processor.Run();
    }

    private static List<Instruction> LoadInstructions(string filePath)
    {
        return File.ReadAllLines(filePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(Instruction.Parse)
                   .ToList();
    }
}