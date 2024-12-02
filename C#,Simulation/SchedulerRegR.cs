
namespace Arquitecture_Project
{
    public class SchedulerRegR
    {

        private Dictionary<string, int> RegsRead = new Dictionary<string, int>();
        private Dictionary<string, int> RegsWritten = new Dictionary<string, int>();

        public SchedulerRegR()
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
            else if (CheckForDependecies(instruction) == 2 || CheckForDependecies(instruction) == 3)
            {
                //If we have a WriteAfterRead or WriteAfterWrite, we will try to apply register renaming
                return ApplyRegisterRenaming(instruction);
            }

            return false;
        }

        private bool ApplyRegisterRenaming(Instruction instruction)
        {
            // Get the destination register
            string destinationRegister = instruction.Dest;

            // Get the register number from the destination register
            int regNum = int.Parse(destinationRegister.Substring(1));
            //Console.WriteLine($"Currently on register R{regNum}");

            // Create renamed register name with S prefix
            string renamedRegister = "S" + regNum;
            //Console.WriteLine($"Renaming {destinationRegister} to {renamedRegister}");

            instruction.Dest = renamedRegister;

            if (CheckForDependecies(instruction) == 0)
            {
                RegsWritten[instruction.Dest] += 1;
                return true;
            }

            renamedRegister = "R" + regNum;

            instruction.Dest = renamedRegister;

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

            RegsRead.Add("S0", 0);
            RegsRead.Add("S1", 0);
            RegsRead.Add("S2", 0);
            RegsRead.Add("S3", 0);
            RegsRead.Add("S4", 0);
            RegsRead.Add("S5", 0);
            RegsRead.Add("S6", 0);
            RegsRead.Add("S7", 0);

            RegsWritten.Add("R0", 0);
            RegsWritten.Add("R1", 0);
            RegsWritten.Add("R2", 0);
            RegsWritten.Add("R3", 0);
            RegsWritten.Add("R4", 0);
            RegsWritten.Add("R5", 0);
            RegsWritten.Add("R6", 0);
            RegsWritten.Add("R7", 0);

            RegsWritten.Add("S0", 0);
            RegsWritten.Add("S1", 0);
            RegsWritten.Add("S2", 0);
            RegsWritten.Add("S3", 0);
            RegsWritten.Add("S4", 0);
            RegsWritten.Add("S5", 0);
            RegsWritten.Add("S6", 0);
            RegsWritten.Add("S7", 0);
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

}