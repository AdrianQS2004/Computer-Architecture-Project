﻿
//This code has the scheduler fully working, the Superscalar in Order works and the Superscalar out of order is also done

namespace Arquitecture_Project
{

    public static class Program
    {
        public static void Main()
        {
            //Console.WriteLine("Current Directory: " + Directory.GetCurrentDirectory());
            //Needs to make sure the file path of the instructions.txt is the correct one, this cannot be shortened 

            var instructions = LoadInstructions(@"C:\Users\jairo\OneDrive\Documentos\GitHub\Computer-Architecture-Project\Instructions-Project\instructions.txt");

            //Runs the processor with single instruction, in order, with one issue slot
            //var processor = new Processor(instructions, 1, 1);

            //Runs the processor with the Superscalar configurations
            //var processorInOrder = new Processor(instructions, 2, 2);
            //var processorOutofOrder = new Processor(instructions, 3, 1);

            //Runs the processor with the Superscalar configurations but it has an implementation of register renaming
            var processorInOrderRegR = new ProcessorRegsR(instructions, 2, 3);
            var processorOutofOrderRegR = new ProcessorRegsR(instructions, 3, 3);
        }

        private static List<Instruction> LoadInstructions(string filePath)
        {
            return File.ReadAllLines(filePath)
                       .Where(line => !string.IsNullOrWhiteSpace(line))
                       .Select(Instruction.Parse)
                       .ToList();
        }
    }
}