namespace Arquitecture_Project
{
    public class Scheduler
    {

        //These two dictionaries help us keep track on which registers are currently being read and written to
        private Dictionary<string, int> RegsRead = new Dictionary<string, int>();
        private Dictionary<string, int> RegsWritten = new Dictionary<string, int>();

        public Scheduler()
        {

            SetUpDictionaries();

        }

        //This method returns true or false if the instruction has a dependency or not
        //It calls another method that actually checks if we have a dependency, but that method returns 
        //which specific type of dependency we have
        public bool IsReady(Instruction instruction)
        {

            if (CheckForDependecies(instruction) == 0)
            {
                UpdateDictionaries(instruction);
                return true;
            }

            return false;
        }

        //This method removes the fact that a register is being read or written to
        //It essentially clears up the reservation of the instruction
        public void RetireInstruction(Instruction instruction)
        {

            RegsWritten[instruction.Dest] -= 1;
            if (instruction.Operator != "Store" && instruction.Operator != "Load")
            {
                RegsRead[instruction.LeftOperand] -= 1;
                RegsRead[instruction.RightOperand] -= 1;
            }
        }

        //Checks if we have a dependency, and if we do it tells us which dependecy it is
        //This will be helpful for register renaming, even though that will be implemented in another class
        private int CheckForDependecies(Instruction instruction)
        {
            //Checks for ReadAfterWrite

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

        //This method is called to reserve which registers are being read and wirtten to
        private void UpdateDictionaries(Instruction instruction)
        {

            RegsWritten[instruction.Dest] += 1;
            if (instruction.Operator != "Store" && instruction.Operator != "Load")
            {
                RegsRead[instruction.LeftOperand] += 1;
                RegsRead[instruction.RightOperand] += 1;
            }

        }

        //this method sets up the table that will be used to make the scheduler work
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

        //This is a debug method that help us make sure the table where being modified correctly
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

}