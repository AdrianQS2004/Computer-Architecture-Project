﻿/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
*/

//This code has the scheduler fully working, the Superscalar in Order works and the Superscalar out of order is also done

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

        RegsWritten[instruction.Dest] -= 1;
        if (instruction.Operator != "Store" && instruction.Operator != "Load")
        {
            RegsRead[instruction.LeftOperand] -= 1;
            RegsRead[instruction.RightOperand] -= 1;
        }
    }

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
                //Console.WriteLine("\nWe have a ReadAfterWrite Dependency\n");
                return 1;


            }
        }
        //Checks the condition for WriteAfterRead
        if (RegsRead[instruction.Dest] != 0)
        {

            //Console.WriteLine("\nWe have a WriteAfterRead Dependency\n");
            return 2;
        }
        //Checks the condition for Write after Write
        else if (RegsWritten[instruction.Dest] != 0)
        {

            //Console.WriteLine("\nWe have a WriteAfterWrite Dependency\n");
            return 3;
        }

        //If it reached the true, it means this has no dependecies
        return 0;

    }

    private void UpdateDictionaries(Instruction instruction)
    {

        RegsWritten[instruction.Dest] += 1;
        if (instruction.Operator != "Store" && instruction.Operator != "Load")
        {
            RegsRead[instruction.LeftOperand] += 1;
            RegsRead[instruction.RightOperand] += 1;
        }

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

    public void PrintDictionaries()
    {
        Console.WriteLine("RegsRead Dictionary:");
        foreach (var entry in RegsRead)
        {
            Console.WriteLine($"{entry.Key}: {entry.Value}");
        }

        Console.WriteLine("\nRegsWritten Dictionary:");
        foreach (var entry in RegsWritten)
        {
            Console.WriteLine($"{entry.Key}: {entry.Value}");
        }
    }
}

public class Processor
{
    private readonly List<Instruction> _instructions;
    private readonly Scheduler _scheduler = new();

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
        int _currentCycle = 0;
        int totalInstructions = _instructions.Count;
        Instruction? currentInstruction = null;
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
        int _currentCycle = 0;
        int totalInstructions = _instructions.Count;
        int multipliedValue = _issue_slots;
        Dictionary<int, int> retireCycles = new Dictionary<int, int>();
        int IssuedColumn = 3 * multipliedValue;

        if (_issue_slots == 3)
        {
            Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-50}{"Retired Instruction",-20}");
            Console.WriteLine(new string('-', 90));
        }
        else
        {
            Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
            Console.WriteLine(new string('-', 60));
        }

        // Run until all instructions are retired
        while (instructionIndex < totalInstructions || retireCycles.Count > 0)
        {
            _currentCycle++;
            string issuedInstruction = "";
            string retiredInstruction = "";

            // Issue instructions if slots are free and not stalled
            for (int i = 0; i < _issue_slots; i++)
            {

                if (instructionIndex >= totalInstructions)
                    break;

                var instruction = _instructions[instructionIndex];

                // Check dependencies before issuing, This method also adds to a table, the registers that were read and written in the current Issued instruction
                if (_scheduler.IsReady(instruction))
                {
                    //If there are no dependencies, This means the instruction can be issued, which is then put in into the retire cyles
                    //The retire Cycles dictionary tells us when an instruction is ready to be retired
                    issuedInstruction += instructionIndex + 1 + "." + instruction.ToString() + " ";
                    retireCycles.Add(instructionIndex + 1, _currentCycle + instruction.CycleCost);
                    instructionIndex++;
                }
                else
                {
                    // Dependency detected, continue progressing cycles
                    //Console.WriteLine($"Cycle {_currentCycle}: Dependency detected for {instruction}");
                    break;
                }

            }

            List<int> cyclesToRemove = new List<int>();

            // Retire instructions that have completed
            foreach (KeyValuePair<int, int> cycle in retireCycles)
            {
                //If a value in retired cycles is the same as the current amount of cycles, then the instructions has been retired
                if (_currentCycle == cycle.Value)
                {
                    //Removes the numbers from the tables in the Scheduler, meaning the registers have been freed up
                    _scheduler.RetireInstruction(_instructions[cycle.Key - 1]);

                    //Sums the retired instructions into the variable that will be printed
                    retiredInstruction += $"Instruction {cycle.Key} ";

                    //Removes the retired instruction from retire cycles
                    cyclesToRemove.Add(cycle.Key);
                }
            }

            foreach (var index in cyclesToRemove.OrderByDescending(x => x))
            {
                retireCycles.Remove(index); // Safely remove cycles after enumeration
            }

            // Print the cycle summary
            if (_issue_slots == 3)
            {
                Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-50}{retiredInstruction,-20}");
            }
            else
            {
                Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");
            }


        }

        if (_issue_slots == 3)
        {
            Console.WriteLine(new string('-', 90));
        }
        else
        {
            Console.WriteLine(new string('-', 60));
        }
        Console.WriteLine("Execution completed.");
    }

    private void Superscalar_out_of_order_run()
    {
        int instructionIndex = 0;
        int _currentCycle = 0;
        int totalInstructions = _instructions.Count;
        Dictionary<int, int> retireCycles = new Dictionary<int, int>();

        Dictionary<int, Instruction> instructionsWithDependency = new Dictionary<int, Instruction>();

        if (_issue_slots == 3)
        {
            Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-50}{"Retired Instruction",-20}");
            Console.WriteLine(new string('-', 90));
        }
        else
        {
            Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
            Console.WriteLine(new string('-', 60));
        }

        // Run until all instructions are retired and until all instructions have been retired 
        while (instructionIndex < totalInstructions || retireCycles.Count > 0 || instructionsWithDependency.Count > 0)
        {

            _currentCycle++;
            string issuedInstruction = "";
            string retiredInstruction = "";

            // Issue instructions if slots are free and not stalled
            for (int i = 0; i < _issue_slots; i++)
            {

                //This condition says that if no instructions are being issued, including those in the instructionwithDependency dictionary
                //then the loop is broken
                if (instructionIndex >= totalInstructions && instructionsWithDependency.Count() == 0)
                {
                    //Console.WriteLine("\nAll instructions have been issued, so we break the loop\n");
                    break;
                }

                //It should now check if an instruction with dependency is ready
                //If any of these instructions is not ready, we continue with everything, it doesnt leave an empty issue slot behind

                //This list allows us to remove instructions from the dependency instruction dictionary that have already been issued
                List<int> InstructionsToRemove = new List<int>();

                //This code only leaves an empty issue slot when the original run of the instructions has a dependency

                foreach (KeyValuePair<int, Instruction> DependencyInstruction in instructionsWithDependency)
                {

                    if (i < _issue_slots)
                    {

                        var instructionD = _instructions[DependencyInstruction.Key];

                        if (_scheduler.IsReady(instructionD))
                        {
                            //If there are no dependencies, This means the instruction can be issued, which is then put in into the retire cyles
                            //The retire Cycles dictionary tells us when an instruction is ready to be retired

                            issuedInstruction += DependencyInstruction.Key + 1 + "." + DependencyInstruction.Value.ToString() + " ";
                            retireCycles.Add(DependencyInstruction.Key, _currentCycle + DependencyInstruction.Value.CycleCost);
                            InstructionsToRemove.Add(DependencyInstruction.Key);
                            i++;
                        }

                    }

                }

                foreach (var IssuedInstruction2 in InstructionsToRemove)
                {
                    instructionsWithDependency.Remove(IssuedInstruction2); // Safely remove instructions from the dictionary after it was issued
                }

                //Makes sure that if two or more issue slots have been used then we can't go in the normal instruction
                if (i > _issue_slots)
                {
                    break;
                }

                //This condition breaks the loop if the instructions have been issued in order were all ran
                if (instructionIndex >= totalInstructions)
                    break;

                var instruction = _instructions[instructionIndex];

                // Check dependencies before issuing, This method also adds to a table, the registers that were read and written in the current Issued instruction
                if (_scheduler.IsReady(instruction))
                {
                    //If there are no dependencies, This means the instruction can be issued, which is then put in into the retire cyles
                    //The retire Cycles dictionary tells us when an instruction is ready to be retired
                    issuedInstruction += instructionIndex + 1 + "." + instruction.ToString() + " ";
                    retireCycles.Add(instructionIndex, _currentCycle + instruction.CycleCost);

                }
                else
                {
                    // Dependency detected, continue progressing cycles
                    //Console.WriteLine($"Cycle {_currentCycle}: Dependency detected for {instruction}");
                    instructionsWithDependency.Add(instructionIndex, instruction);
                    //break;
                }

                //Instruction index is summed up no matter what, specially because it is out of order
                instructionIndex++;

            }

            //Instructions are retired no matter how many are done or if they are in a different order

            List<int> cyclesToRemove = new List<int>();

            // Retire instructions that have completed
            foreach (KeyValuePair<int, int> cycle in retireCycles)
            {
                //If a value in retired cycles is the same as the current amount of cycles, then the instructions has been retired
                if (_currentCycle == cycle.Value)
                {
                    //Removes the numbers from the tables in the Scheduler, meaning the registers have been freed up
                    _scheduler.RetireInstruction(_instructions[cycle.Key]);

                    //Sums the retired instructions into the variable that will be printed
                    retiredInstruction += $"Instruction {cycle.Key + 1} ";

                    //Removes the retired instruction from retire cycles
                    cyclesToRemove.Add(cycle.Key);
                }
            }

            foreach (var index in cyclesToRemove)
            {
                retireCycles.Remove(index); // Safely remove cycles after enumeration
            }

            // Print the cycle summary

            if (_issue_slots == 3)
            {
                Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-50}{retiredInstruction,-20}");
            }
            else
            {
                Console.WriteLine($"{_currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");
            }

        }

        if (_issue_slots == 3)
        {
            Console.WriteLine(new string('-', 90));
        }
        else
        {
            Console.WriteLine(new string('-', 60));
        }

        Console.WriteLine("Execution completed.");
    }
}

public static class Program
{
    public static void Main()
    {
        //Console.WriteLine("Current Directory: " + Directory.GetCurrentDirectory());
        //Needs to make sure the file path of the instructions.txt is the correct one, this cannot be shortened 

        var instructions = LoadInstructions(@"C:\Users\jairo\OneDrive\Documentos\GitHub\Computer-Architecture-Project\Instructions-Project\instructions.txt");

        //Runs the processor with single instruction, in order, with one issue slot
        var processor = new Processor(instructions, 1, 1);
        var processorInOrder = new Processor(instructions, 2, 2);
        var processorOutofOrder = new Processor(instructions, 3, 3);

    }

    private static List<Instruction> LoadInstructions(string filePath)
    {
        return File.ReadAllLines(filePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(Instruction.Parse)
                   .ToList();
    }
}