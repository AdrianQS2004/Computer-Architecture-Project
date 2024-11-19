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
    public readonly Dictionary<string, int> _busyUntil = new();
    private readonly HashSet<string> _readyOperands = new();

    public bool IsReady(Instruction instruction, int currentCycle, out string dependency)
    {
        dependency = null;

        if (!string.IsNullOrEmpty(instruction.LeftOperand) && _busyUntil.TryGetValue(instruction.LeftOperand, out var leftBusyUntil))
        {
            if (currentCycle < leftBusyUntil)
            {
                dependency = instruction.LeftOperand;
                return false;
            }
        }

        if (!string.IsNullOrEmpty(instruction.RightOperand) && _busyUntil.TryGetValue(instruction.RightOperand, out var rightBusyUntil))
        {
            if (currentCycle < rightBusyUntil)
            {
                dependency = instruction.RightOperand;
                return false;
            }
        }

        return true;
    }

    public void Reserve(Instruction instruction, int currentCycle)
    {
        _busyUntil[instruction.Dest] = currentCycle + instruction.CycleCost;
        _readyOperands.Add(instruction.Dest);
    }

    public bool IsOperandReady(string operand) => _readyOperands.Contains(operand);
}

public class Processor
{
    private readonly List<Instruction> _instructions;
    private readonly Scheduler _scheduler = new();
    private int _currentCycle;
    //private HashSet<int> _retired = new();
    //private Instruction _inFlight;
    //private bool _waitForRetire;

    private int _issueSlots;

    public Processor(List<Instruction> instructions, int Configuration, int issue_slots)
    {
        _instructions = instructions;
        _issueSlots = issue_slots;

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
        Queue<Instruction> inFlight = new();
        List<string> issuedInstructions = new();
        List<string> retiredInstructions = new();

        Console.WriteLine($"{"Cycle",-10}{"Issued Instructions",-30}{"Retired Instructions",-20}");
        Console.WriteLine(new string('-', 60));

        while (instructionIndex < totalInstructions || inFlight.Count > 0)
        {
            _currentCycle++;
            issuedInstructions.Clear();
            retiredInstructions.Clear();

            // Retire completed instructions
            while (inFlight.Count > 0 && _currentCycle >= _scheduler._busyUntil[inFlight.Peek().Dest])
            {
                var instruction = inFlight.Dequeue();
                retiredInstructions.Add($"Instruction {_instructions.IndexOf(instruction) + 1}");
            }

            // Issue new instructions (up to the number of issue slots)
            int issuedCount = 0;
            while (issuedCount < _issueSlots && instructionIndex < totalInstructions)
            {
                var instruction = _instructions[instructionIndex];
                if (_scheduler.IsReady(instruction, _currentCycle, out var dependency))
                {
                    issuedInstructions.Add(instruction.ToString());
                    _scheduler.Reserve(instruction, _currentCycle);
                    inFlight.Enqueue(instruction);
                    instructionIndex++;
                    issuedCount++;
                }
                else
                {
                    Console.WriteLine($"Cycle {_currentCycle}: Dependency on {dependency} stalls issuing of {instruction}");
                    break; // Stop trying to issue if there is a dependency
                }
            }

            Console.WriteLine($"{_currentCycle,-10}{string.Join("; ", issuedInstructions),-30}{string.Join("; ", retiredInstructions),-20}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Execution completed.");
    }

    private void Superscalar_out_of_order_run()
    {
        int instructionIndex = 0;
        int totalInstructions = _instructions.Count;
        List<Instruction> readyQueue = new();
        Queue<Instruction> inFlight = new();
        List<string> issuedInstructions = new();
        List<string> retiredInstructions = new();

        Console.WriteLine($"{"Cycle",-10}{"Issued Instructions",-30}{"Retired Instructions",-20}");
        Console.WriteLine(new string('-', 60));

        while (instructionIndex < totalInstructions || inFlight.Count > 0 || readyQueue.Count > 0)
        {
            _currentCycle++;
            issuedInstructions.Clear();
            retiredInstructions.Clear();

            // Retire completed instructions
            var retiring = inFlight.Where(i => _currentCycle >= _scheduler._busyUntil[i.Dest]).ToList();
            foreach (var instruction in retiring)
            {
                retiredInstructions.Add($"Instruction {_instructions.IndexOf(instruction) + 1}");
                inFlight.Dequeue();
            }

            // Move ready instructions to the ready queue
            while (instructionIndex < totalInstructions)
            {
                var instruction = _instructions[instructionIndex];
                if (_scheduler.IsReady(instruction, _currentCycle, out _))
                {
                    readyQueue.Add(instruction);
                    instructionIndex++;
                }
                else
                {
                    break; // Stop adding to the ready queue if dependencies are unresolved
                }
            }

            // Issue instructions from the ready queue (up to the number of issue slots)
            int issuedCount = 0;
            foreach (var instruction in readyQueue.ToList())
            {
                if (issuedCount >= _issueSlots) break;

                if (_scheduler.IsReady(instruction, _currentCycle, out _))
                {
                    issuedInstructions.Add(instruction.ToString());
                    _scheduler.Reserve(instruction, _currentCycle);
                    inFlight.Enqueue(instruction);
                    readyQueue.Remove(instruction);
                    issuedCount++;
                }
            }

            Console.WriteLine($"{_currentCycle,-10}{string.Join("; ", issuedInstructions),-30}{string.Join("; ", retiredInstructions),-20}");
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
        var InOrderprocessor = new Processor(instructions, 2, 1);
        var OutOfOrderprocessor = new Processor(instructions, 3, 1);
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