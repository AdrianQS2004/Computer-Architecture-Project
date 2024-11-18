/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
*/


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
            ? $"{Dest} = {Operator}, CycleCost = {CycleCost}"
            : $"{Dest} = {LeftOperand} {Operator} {RightOperand}, CycleCost = {CycleCost}";
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
    private Instruction _inFlight;
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

        Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
        Console.WriteLine(new string('-', 60));

        while (instructionIndex < totalInstructions || _inFlight != null)
        {
            _currentCycle++;
            string issuedInstruction = "";
            string retiredInstruction = "";

            if (_inFlight != null)
            {
                if (_scheduler.IsReady(_inFlight, _currentCycle))
                {
                    int retiredIndex = _instructions.IndexOf(_inFlight) + 1;
                    retiredInstruction = $"Instruction {retiredIndex}";
                    _retired.Add(retiredIndex);
                    _inFlight = null;
                    _waitForRetire = true;
                }
            }

            if (_inFlight == null && !_waitForRetire && instructionIndex < totalInstructions)
            {
                var nextInstruction = _instructions[instructionIndex];
                if (_scheduler.IsReady(nextInstruction, _currentCycle))
                {
                    issuedInstruction = nextInstruction.ToString();
                    _scheduler.Reserve(nextInstruction, _currentCycle);
                    _inFlight = nextInstruction;
                    instructionIndex++;
                }
            }

            if (_waitForRetire && string.IsNullOrEmpty(issuedInstruction))
            {
                _waitForRetire = false;
            }

            Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
    }

    private void Superscalar_in_order_run()
    {

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

        var processor = new Processor(instructions, 1, 1);
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

