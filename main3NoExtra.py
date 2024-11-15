# Define the cycle costs for different operations
CYCLE_COST = {
    '+': 1,
    '-': 1,
    '*': 2
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
        # Parse an instruction from a line of text
        dest, expr = line.split(' = ')
        left_operand, operator, right_operand = expr.split()
        cycle_cost = CYCLE_COST[operator.strip()]
        return cls(dest.strip(), left_operand.strip(), operator.strip(), right_operand.strip(), cycle_cost)

    def __str__(self):
        # Return the string representation of the instruction in the format "dest = left_operand operator right_operand"
        return f"{self.dest} = {self.left_operand} {self.operator} {self.right_operand}"

class RegisterFile:
    def __init__(self):
        # Initialize 8 registers R0 to R7 with zero values
        self.registers = {f"R{i}": 0 for i in range(8)}

    def execute(self, instruction):
        # Execute an instruction and update the register file
        if instruction.operator == '+':
            self.registers[instruction.dest] = self.registers[instruction.left_operand] + self.registers[instruction.right_operand]
        elif instruction.operator == '-':
            self.registers[instruction.dest] = self.registers[instruction.left_operand] - self.registers[instruction.right_operand]
        elif instruction.operator == '*':
            self.registers[instruction.dest] = self.registers[instruction.left_operand] * self.registers[instruction.right_operand]

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
