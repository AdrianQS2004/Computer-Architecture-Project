# Define the cycle costs for different operations
CYCLE_COST = {
    '+': 1,
    '-': 1,
    '*': 2,
    'Store': 3,  # Store operation cycle cost (simulated)
    'Load': 3    # Load operation cycle cost (simulated)
}

class Instruction:
    def __init__(self, dest, left_operand, operator, right_operand, cycle_cost):
        self.dest = dest
        self.left_operand = left_operand
        self.operator = operator
        self.right_operand = right_operand
        self.cycle_cost = cycle_cost

    @classmethod
    def parse(cls, line):
        # Split the line into two parts: before and after the equals sign
        parts = line.split(' = ')
        dest = parts[0].strip()
        expression = parts[1].strip()

        # Check if the operator is Store or Load (these are special instructions)
        if expression == 'Store' or expression == 'Load':
            # These are special instructions, where the operator is either Store or Load
            return cls(dest, None, expression, None, CYCLE_COST[expression])

        # Handle arithmetic instructions (must contain left operand, operator, right operand)
        expr = expression.split()
        if len(expr) != 3:
            raise ValueError(f"Invalid arithmetic instruction format: {line}")

        left_operand, operator, right_operand = expr
        cycle_cost = CYCLE_COST[operator.strip()]
        return cls(dest.strip(), left_operand.strip(), operator.strip(), right_operand.strip(), cycle_cost)

    def __str__(self):
        # Return the string representation of the instruction
        if self.operator in ['Store', 'Load']:
            return f"{self.dest} = {self.operator}"
        return f"{self.dest} = {self.left_operand} {self.operator} {self.right_operand}"

# Function to load and parse instructions from a file
def load_instructions(file_path):
    # Load instructions from a file and parse them into Instruction objects
    with open(file_path, 'r') as f:
        instructions = [Instruction.parse(line.strip()) for line in f if line.strip()]
    return instructions

# Load instructions and print them
instructions = load_instructions("instructions.txt")

# Print the instructions exactly as they are in the file
for instruction in instructions:
    print(instruction)
