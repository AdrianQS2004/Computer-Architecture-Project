/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
*/

//This code only has a first version of superscalar
//Is not working as intended
//It seems it is not staling the code when it needs to

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
    private readonly Dictionary<string, int> _busyUntil = new();

    public bool IsReady(Instruction instruction, int currentCycle)
    {
        if (!string.IsNullOrEmpty(instruction.LeftOperand) && _busyUntil.TryGetValue(instruction.LeftOperand, out var leftBusyUntil))
        {
            if (currentCycle < leftBusyUntil) return false;
        }

        if (!string.IsNullOrEmpty(instruction.RightOperand) && _busyUntil.TryGetValue(instruction.RightOperand, out var rightBusyUntil))
        {
            if (currentCycle < rightBusyUntil) return false;
        }

        return true;
    }

    public void Reserve(Instruction instruction, int currentCycle)
    {
        _busyUntil[instruction.Dest] = currentCycle + instruction.CycleCost;
    }
}

public class Processor
{
    private readonly List<Instruction> _instructions;
    private readonly Scheduler _scheduler = new();
    private int _currentCycle;
    private HashSet<int> _retired = new();
    //private Instruction _inFlight;
    private bool _waitForRetire;

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
        int totalInstructions = _instructions.Count;
        int instructionIndex = 0;
        var inFlightInstructions = new List<(Instruction instruction, int retireCycle)>();

        Console.WriteLine($"{"Cycle",-10}{"Issued Instructions",-30}{"Retired Instructions",-20}");
        Console.WriteLine(new string('-', 60));

        while (instructionIndex < totalInstructions || inFlightInstructions.Any())
        {
            _currentCycle++;
            var issuedInstructions = new List<string>();
            var retiredInstructions = new List<string>();

            // Retire instructions that have completed
            for (int i = inFlightInstructions.Count - 1; i >= 0; i--)
            {
                var (instruction, retireCycle) = inFlightInstructions[i];
                if (_currentCycle >= retireCycle)
                {
                    int retiredIndex = _instructions.IndexOf(instruction) + 1;
                    retiredInstructions.Add($"Instruction {retiredIndex}");
                    inFlightInstructions.RemoveAt(i);
                }
            }

            // Issue instructions up to the issue slot limit
            while (inFlightInstructions.Count < _issue_slots && instructionIndex < totalInstructions)
            {
                var instruction = _instructions[instructionIndex];
                if (_scheduler.IsReady(instruction, _currentCycle))
                {
                    issuedInstructions.Add(instruction.ToString());
                    _scheduler.Reserve(instruction, _currentCycle);
                    inFlightInstructions.Add((instruction, _currentCycle + instruction.CycleCost));
                    instructionIndex++;
                }
                else
                {
                    break; // Dependencies are not resolved; stop issuing
                }
            }

            Console.WriteLine($"{_currentCycle,-10}{string.Join(", ", issuedInstructions),-30}{string.Join(", ", retiredInstructions),-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
    }

    private void Superscalar_out_of_order_run()
    {
        int totalInstructions = _instructions.Count;
        var readyQueue = new Queue<Instruction>();
        var inFlightInstructions = new List<(Instruction instruction, int retireCycle)>();
        var issuedIndices = new HashSet<int>();

        Console.WriteLine($"{"Cycle",-10}{"Issued Instructions",-30}{"Retired Instructions",-20}");
        Console.WriteLine(new string('-', 60));

        while (issuedIndices.Count < totalInstructions || inFlightInstructions.Any())
        {
            _currentCycle++;
            var issuedInstructions = new List<string>();
            var retiredInstructions = new List<string>();

            // Retire instructions that have completed
            for (int i = inFlightInstructions.Count - 1; i >= 0; i--)
            {
                var (instruction, retireCycle) = inFlightInstructions[i];
                if (_currentCycle >= retireCycle)
                {
                    int retiredIndex = _instructions.IndexOf(instruction) + 1;
                    retiredInstructions.Add($"Instruction {retiredIndex}");
                    inFlightInstructions.RemoveAt(i);
                }
            }

            // Populate ready queue with all ready instructions
            for (int i = 0; i < totalInstructions; i++)
            {
                if (!issuedIndices.Contains(i) && _scheduler.IsReady(_instructions[i], _currentCycle))
                {
                    readyQueue.Enqueue(_instructions[i]);
                    issuedIndices.Add(i); // Mark instruction as queued
                }
            }

            // Issue instructions from the ready queue up to the issue slot limit
            while (readyQueue.Count > 0 && inFlightInstructions.Count < _issue_slots)
            {
                var instruction = readyQueue.Dequeue();
                issuedInstructions.Add(instruction.ToString());
                _scheduler.Reserve(instruction, _currentCycle);
                inFlightInstructions.Add((instruction, _currentCycle + instruction.CycleCost));
            }

            Console.WriteLine($"{_currentCycle,-10}{string.Join(", ", issuedInstructions),-30}{string.Join(", ", retiredInstructions),-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
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
        var processor2 = new Processor(instructions, 2, 1);
        var processor3 = new Processor(instructions, 3, 1);
    }

    private static List<Instruction> LoadInstructions(string filePath)
    {
        return File.ReadAllLines(filePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(Instruction.Parse)
                   .ToList();
    }
}