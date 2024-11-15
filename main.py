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

class Processor:
    def __init__(self, instructions):
        self.instructions = instructions  # List of instructions
        self.register_file = RegisterFile()
        self.cycle = 1
        self.issued_instruction_index = 0  # Tracks the index of the next instruction to issue
        self.active_instruction = None  # Currently active instruction
        self.retire_cycles = {}  # Maps instruction index to retire cycle

    def simulate(self):
        # Define header with fixed-width columns
        print(f"{'Cycle':<10}{'Issued':<20}{'Retired':<20}")
        
        # Loop until all instructions are retired
        while self.issued_instruction_index < len(self.instructions) or self.active_instruction is not None:
            # Check if an instruction can be retired
            retired_instruction = "None"
            if self.active_instruction is not None and self.cycle == self.retire_cycles[self.active_instruction]:
                retired_instruction = f"Instruction {self.active_instruction + 1}"
                self.active_instruction = None  # Free up for the next instruction to issue

            # Issue the next instruction if possible
            issued = "None"
            if self.active_instruction is None and self.issued_instruction_index < len(self.instructions):
                instruction = self.instructions[self.issued_instruction_index]
                self.retire_cycles[self.issued_instruction_index] = self.cycle + instruction.cycle_cost
                self.active_instruction = self.issued_instruction_index
                issued = f"Instruction {self.issued_instruction_index + 1}"
                self.issued_instruction_index += 1

            # Print the cycle status with fixed-width columns for alignment
            print(f"{self.cycle:<10}{issued:<20}{retired_instruction:<20}")

            # Advance the cycle
            self.cycle += 1

def load_instructions(file_path):
    # Load instructions from a file and parse them into Instruction objects
    with open(file_path, 'r') as f:
        return [Instruction.parse(line.strip()) for line in f if line.strip()]

# Load instructions and run the simulation
instructions = load_instructions("instructions.txt")
processor = Processor(instructions)
processor.simulate()
