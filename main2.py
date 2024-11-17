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

class Scheduler:
    def __init__(self):
        self.busy_until = {}  # Tracks when each destination will be free

    def is_ready(self, instruction, current_cycle):
        # Check if dependencies (left_operand and right_operand) are resolved
        if instruction.left_operand and instruction.left_operand in self.busy_until:
            if current_cycle < self.busy_until[instruction.left_operand]:
                return False
        if instruction.right_operand and instruction.right_operand in self.busy_until:
            if current_cycle < self.busy_until[instruction.right_operand]:
                return False
        return True

    def reserve(self, instruction, current_cycle):
        # Reserve the destination until the instruction's cycle cost completes
        self.busy_until[instruction.dest] = current_cycle + instruction.cycle_cost


class Processor:
    def __init__(self, instructions):
        self.instructions = instructions
        self.scheduler = Scheduler()
        self.current_cycle = 0
        self.retired = set()  # Tracks indices of retired instructions
        self.in_flight = None  # Currently issued instruction
        self.wait_for_retire = False  # Flag to prevent issuing in the same cycle as retiring

    def run(self):
        instruction_index = 0
        total_instructions = len(self.instructions)

        # Print the header
        print(f"{'Cycle':<10}{'Issued Instruction':<30}{'Retired Instruction':<20}")
        print("-" * 60)

        while instruction_index < total_instructions or self.in_flight:
            self.current_cycle += 1

            issued_instruction = ""
            retired_instruction = ""

            # Retire the instruction if its execution is completed
            if self.in_flight:
                dest = self.in_flight.dest
                if self.scheduler.busy_until[dest] == self.current_cycle:
                    retired_index = self.instructions.index(self.in_flight) + 1
                    retired_instruction = f"Instruction {retired_index}"
                    self.retired.add(retired_index)
                    self.in_flight = None  # Clear the in-flight instruction
                    self.wait_for_retire = True  # Ensure next cycle does not issue immediately

            # Issue the next instruction if no instruction is in flight and no recent retirement
            if not self.in_flight and not self.wait_for_retire and instruction_index < total_instructions:
                next_instruction = self.instructions[instruction_index]
                if self.scheduler.is_ready(next_instruction, self.current_cycle):
                    issued_instruction = str(next_instruction)
                    self.scheduler.reserve(next_instruction, self.current_cycle)
                    self.in_flight = next_instruction
                    instruction_index += 1

            # Reset the wait flag after a cycle with no issuing
            if self.wait_for_retire and not issued_instruction:
                self.wait_for_retire = False

            # Print the current cycle's details in columns
            print(f"{self.current_cycle:<10}{issued_instruction:<30}{retired_instruction:<20}")

        print("-" * 60)
        print("Execution completed.")


# Load instructions and run the processor
instructions = load_instructions("instructions.txt")
processor = Processor(instructions)
processor.run()




