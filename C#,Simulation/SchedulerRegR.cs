
namespace Arquitecture_Project
{
    public class SchedulerRegR
    {
        //These two dictionaries help us keep track on which registers are currently being read and written to
        private Dictionary<string, int> RegsRead = new Dictionary<string, int>();
        private Dictionary<string, int> RegsWritten = new Dictionary<string, int>();

        //This class is very similar but has some key differences to the normal scheduler class
        //This version of the scheduler implements register renaming, so I will only explain the new methods
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

        //This method will apply to the destination of the instruction the main idea of register renaming
        //The destination will be changed to one of the S registers to avoid the WAR and WAW dependencies
        //This method has a major flaw that happens very little, but it because of the ambiguity of how register renaming works, I keep it as is,
        //Siimply, the flaw can only be changed if have different logic for the register renaming, and I wasn't able to change that logic
        //The error simply is because of the way I issue the renamed registers.
        //Following the case study from the professor, the renamed registers should be issued as a stack. Meaning in the first renamed register we issue S0, no matter which register it came from
        //This helps us to make sure that if we have two WA dependencies on the same register, we can still issue both instructions using renamed registers
        //My logic does not allow this. Because in my logic the renamed register is chosen by the number of the register that needs to be renamed
        //Meaning if we have a WA on R3, we change the register to S3. Now this works mostly, the main problem is that if we get another
        //WA on R3 while S3 is being written, we will get a delay on a WA dependency. This is definetly not ideal. 
        //I could implemente the stack logic, the problem is that
        //keeping charge on the logic that the regiter renaming rules method. 
        //I simply coulnd't find a way to update the read registers to the reanmed registers if we followed this logic
        //I hope this error is understandable.
        private bool ApplyRegisterRenaming(Instruction instruction)
        {
            // Get the destination register
            string destinationRegister = instruction.Dest;

            // Get the register number from the destination register
            int regNum = int.Parse(destinationRegister.Substring(1));

            // Create renamed register name with S prefix
            string renamedRegister = "S" + regNum;
            //Console.WriteLine($"Renaming {destinationRegister} to {renamedRegister}");

            instruction.Dest = renamedRegister;

            //If the S register is not being used, we reserve the register in the table
            if (CheckForDependecies(instruction) == 0)
            {
                UpdateDictionaries(instruction);
                //PrintDictionaries();
                return true;
            }

            //If for some reason the S register being called up is occupied, then we change the instruction
            //dest variable to the original one
            renamedRegister = "R" + regNum;

            instruction.Dest = renamedRegister;

            return false;
        }
        //THis method will change the values that need to be read with the new register
        //This logic works and simulates the read after dependencies if the renamed register is used
        //It has problem it we try to think about the register having actual values, for if we ignore it, we can mostly get the correct amount of cycles
        public void RegisterRenamingRules(Instruction instruction)
        {

            string LeftRegisterRR = instruction.LeftOperand;

            // Get the register number from the destination register
            int regNum1 = int.Parse(LeftRegisterRR.Substring(1));

            string RightRegisterRR = instruction.RightOperand;

            int regNum2 = int.Parse(RightRegisterRR.Substring(1));

            // Create renamed register name with S prefix
            string renamedRegisterLeft = "S" + regNum1;
            string renamedRegisterRight = "S" + regNum2;

            //If the renamed register is being written to, then we change the value of the instruction that was going to be issued
            //We check both operand registers
            if (RegsWritten[renamedRegisterLeft] == 1)
            {
                instruction.LeftOperand = renamedRegisterLeft;
            }

            if (RegsWritten[renamedRegisterRight] == 1)
            {
                instruction.RightOperand = renamedRegisterRight;
            }

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

        //We add the new digital registers that will help us execute register renaming
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