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
    private Instruction _inFlight;
    private bool _waitForRetire;

    public Processor(List<Instruction> instructions)
    {
        _instructions = instructions;
    }

    public void Run()
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
}

public static class Program
{
    public static void Main()
    {
        var instructions = LoadInstructions("C:\\Users\\jairo\\OneDrive\\Documentos\\GitHub\\Computer-Architecture-Project\\instructions.txt");
        var processor = new Processor(instructions);
        processor.Run();
    }

    private static List<Instruction> LoadInstructions(string filePath)
    {
        return File.ReadAllLines(filePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(Instruction.Parse)
                   .ToList();
    }
}

