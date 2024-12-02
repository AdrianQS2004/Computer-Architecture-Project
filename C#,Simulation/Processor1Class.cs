namespace Arquitecture_Project
{

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

            PrintCycleHeader(_issue_slots);

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

                    // Check if the instruction has dependencies before issuing
                    if (_scheduler.IsReady(instruction))
                    {
                        //If there are no dependencies, This means the instruction can be issued, which is then put in into the retire cyles
                        //The retire Cycles dictionary tells us when an instruction is ready to be retired

                        if (instructionIndex < 9)
                        {
                            issuedInstruction += instructionIndex + 1 + ". " + instruction.ToString() + "  ";
                        }
                        else
                        {
                            issuedInstruction += instructionIndex + 1 + "." + instruction.ToString() + "  ";
                        }


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

                //This part of the code retires instructions in order
                //In this list we add instructions that were retired to remove from the retire cycles dictionary
                List<int> cyclesToRemove = new List<int>();

                // Retire instructions that have completed
                // What the loop does is that it searches for the lowest number that was issued, and if it can't be retired yet
                // the loop breaks, meaning we need to retire instructions in order to print whats needed

                foreach (var key in retireCycles.Keys.OrderBy(x => x))
                {
                    if (_currentCycle >= retireCycles[key])
                    {
                        // Removes the numbers from the tables in the Scheduler, meaning the registers have been freed up
                        _scheduler.RetireInstruction(_instructions[key - 1]);

                        // Sums the retired instructions into the variable that will be printed
                        retiredInstruction += $"Instruction {key} ";

                        // Adds the retired instruction to the list for removal
                        cyclesToRemove.Add(key);
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var index in cyclesToRemove.OrderByDescending(x => x))
                {
                    retireCycles.Remove(index); // Safely remove cycles after enumeration
                }

                // Print the cycle summary
                PrintCycleSummary(_currentCycle, issuedInstruction, retiredInstruction, _issue_slots);


            }

            if (_issue_slots == 3)
            {
                Console.WriteLine(new string('-', 100));
            }
            else
            {
                Console.WriteLine(new string('-', 70));
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

            PrintCycleHeader(_issue_slots);

            // Run until all instructions are retired and until all instructions with dependencies have been retired 
            while (instructionIndex < totalInstructions || retireCycles.Count > 0 || instructionsWithDependency.Count > 0)
            {

                _currentCycle++;
                string issuedInstruction = "";
                string retiredInstruction = "";

                // Tries to issues as many instructions in the cycle as the amount of issue slots
                for (int i = 0; i < _issue_slots; i++)
                {

                    //This condition says that if no instructions is being issued, including those in the instructionwithDependency dictionary
                    //then the loop is broken, helps us avoid entering a dictionary that simply doesn't exist
                    if (instructionIndex >= totalInstructions && instructionsWithDependency.Count() == 0)
                    {
                        //Console.WriteLine("\nAll instructions have been issued, so we break the loop\n");
                        break;
                    }

                    //It should now check if an instruction with dependency is ready
                    //If any of these instructions is not ready, we continue with everything

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
                                if (DependencyInstruction.Key < 9)
                                {
                                    issuedInstruction += DependencyInstruction.Key + 1 + ". " + DependencyInstruction.Value.ToString() + "  ";
                                }
                                else
                                {
                                    issuedInstruction += DependencyInstruction.Key + 1 + "." + DependencyInstruction.Value.ToString() + "  ";
                                }

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

                    //Console.WriteLine("\n This is the amount of issue slots used by the special Dictionary: " + i + "\n");

                    //Makes sure that if two or more issue slots have been used then we can't go in the normal instruction
                    if (i >= _issue_slots)
                    {
                        break;
                    }

                    //This condition breaks the loop if the instructions all been issued
                    if (instructionIndex >= totalInstructions)
                        break;

                    var instruction = _instructions[instructionIndex];

                    // Check dependencies before issuing
                    if (_scheduler.IsReady(instruction))
                    {
                        //If there are no dependencies, This means the instruction can be issued, which is then put in into the retire cyles
                        //The retire Cycles dictionary tells us when an instruction is ready to be retired

                        if (instructionIndex < 9)
                        {
                            issuedInstruction += instructionIndex + 1 + ". " + instruction.ToString() + "  ";
                        }
                        else
                        {
                            issuedInstruction += instructionIndex + 1 + "." + instruction.ToString() + "  ";
                        }

                        //issuedInstruction += instructionIndex + 1 + "." + instruction.ToString() + " ";


                        retireCycles.Add(instructionIndex, _currentCycle + instruction.CycleCost);

                    }
                    else
                    {
                        // Dependency detected, 
                        //Console.WriteLine($"Cycle {_currentCycle}: Dependency detected for {instruction}");
                        // When a dependency is detected, we add it this dictionary so that in the next cycle
                        // we can try to issue the instruction with dependency
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

                PrintCycleSummary(_currentCycle, issuedInstruction, retiredInstruction, _issue_slots);

            }

            if (_issue_slots == 3)
            {
                Console.WriteLine(new string('-', 100));
            }
            else
            {
                Console.WriteLine(new string('-', 70));
            }

            Console.WriteLine("Execution completed.");
        }

        //These next functions help us print different sizes of spaces so that it looks clean
        //No matter the amount of issue slots choosen
        private void PrintCycleSummary(int currentCycle, string issuedInstruction, string retiredInstruction, int issueSlots)
        {

            if (issueSlots == 3)
            {
                Console.WriteLine($"{currentCycle,-10}{issuedInstruction,-55}{retiredInstruction,-20}");
            }
            else if (issueSlots == 2)
            {
                Console.WriteLine($"{currentCycle,-10}{issuedInstruction,-40}{retiredInstruction,-20}");
            }
            else
            {
                Console.WriteLine($"{currentCycle,-10}{issuedInstruction,-30}{retiredInstruction,-20}");

            }
        }

        private void PrintCycleHeader(int issueSlots)
        {
            if (issueSlots == 3)
            {
                Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-55}{"Retired Instruction",-20}");
                Console.WriteLine(new string('-', 100));
            }
            else if (issueSlots == 2)
            {
                Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-40}{"Retired Instruction",-20}");
                Console.WriteLine(new string('-', 70));
            }
            else
            {
                Console.WriteLine($"{"Cycle",-10}{"Issued Instruction",-30}{"Retired Instruction",-20}");
                Console.WriteLine(new string('-', 60));
            }
        }

    }
}