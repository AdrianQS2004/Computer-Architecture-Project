namespace Arquitecture_Project
{
    public class Instruction
    {
        public string Dest { get; set; }
        public string LeftOperand { get; set; }
        public string Operator { get; }
        public string RightOperand { get; set; }
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

        //Parses an instruction from the text file and saves it in the instruction class
        public static Instruction Parse(string line)
        {
            var parts = line.Split(" = ", StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid instruction format: {line}");

            var dest = parts[0];
            var expression = parts[1];

            if (expression == "Store" || expression == "Load")
            {
                return new Instruction(dest, "", expression, "", CycleCostEnum[expression]);
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

        //Simply a function that helps us print the values inside of this class
        public override string ToString()
        {
            if (Operator == "Store")
            {
                return $"{Dest} = {Operator}  ";
            }
            else if (Operator == "Load")
            {
                return $"{Dest} = {Operator}   ";
            }
            else
            {
                return $"{Dest} = {LeftOperand} {Operator} {RightOperand}";
            }
        }
    }
}