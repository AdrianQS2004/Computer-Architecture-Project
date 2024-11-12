class Instruction:
    def __init__(self, name, inst_type, dependencies):
        """
        Initialize an instruction.

        Parameters:
        - name: Name of the instruction (e.g., 'I1', 'I2')
        - inst_type: Type of the instruction (e.g., 'ALU', 'LOAD', 'STORE')
        - dependencies: List of instructions that must complete before this one
        """
        self.name = name
        self.inst_type = inst_type
        self.dependencies = dependencies
        self.issue_cycle = None

    def __repr__(self):
        return f"{self.name}({self.inst_type})"


class Processor:
    def __init__(self, issue_width):
        """
        Initialize the processor.

        Parameters:
        - issue_width: Maximum number of instructions that can be issued per cycle
        """
        self.issue_width = issue_width
        self.current_cycle = 0

    def can_issue(self, instruction, completed_instructions):
        """
        Determine if an instruction can be issued based on dependencies and issue width.

        Parameters:
        - instruction: The instruction to check
        - completed_instructions: Set of completed instruction names
        """
        # Check if all dependencies are met
        for dependency in instruction.dependencies:
            if dependency not in completed_instructions:
                return False
        return True


class Scheduler:
    def __init__(self, processor):
        """
        Initialize the scheduler.

        Parameters:
        - processor: The processor object that schedules instructions
        """
        self.processor = processor
        self.schedule = []

    def schedule_instructions(self, instructions):
        """
        Schedule a list of instructions based on dependencies and issue width.

        Parameters:
        - instructions: List of Instruction objects to schedule
        """
        instructions_to_schedule = instructions[:]
        completed_instructions = set()

        while instructions_to_schedule:
            # Start a new cycle
            cycle_instructions = []
            self.processor.current_cycle += 1

            for instruction in instructions_to_schedule[:]:
                if (len(cycle_instructions) < self.processor.issue_width and
                        self.processor.can_issue(instruction, completed_instructions)):
                    # Schedule the instruction in the current cycle
                    instruction.issue_cycle = self.processor.current_cycle
                    cycle_instructions.append(instruction)
                    completed_instructions.add(instruction.name)
                    instructions_to_schedule.remove(instruction)

            # Append scheduled instructions for this cycle
            self.schedule.append((self.processor.current_cycle, cycle_instructions))

    def print_schedule(self):
        """Print the schedule showing instructions issued in each cycle."""
        print("Cycle\tInstructions")
        for cycle, instructions in self.schedule:
            print(f"{cycle}\t{', '.join(map(str, instructions))}")


# Example usage
instructions = [
    Instruction("I1", "ALU", []),         # No dependencies
    Instruction("I2", "LOAD", ["I1"]),    # Depends on I1
    Instruction("I3", "STORE", ["I1"]),   # Depends on I1
    Instruction("I4", "ALU", ["I2", "I3"])  # Depends on I2 and I3
]

processor = Processor(issue_width=2)  # Processor can issue 2 instructions per cycle
scheduler = Scheduler(processor)

scheduler.schedule_instructions(instructions)
scheduler.print_schedule()
