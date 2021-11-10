using System;
using System.Collections.Generic;
using System.Text;

namespace BrainfuckRun {
    public record BrainfuckProgram(IList<Instruction> Instructions) {
        private int ip = 0;

        public void Run(Tape tape, ITerminal io) {
            for (; ip < Instructions.Count; ip++) {
                Instructions[ip].Run(tape, io);
            }
            ip = 0;
        }

        public void Serialize(StringBuilder builder, bool optimize) {
            foreach (var item in Instructions) item.Serialize(builder, optimize);
        }
    }

    public abstract record Instruction(string Source, int Offset) {
        public abstract void Run(Tape tape, ITerminal io);
        public abstract void Serialize(StringBuilder builder, bool usePGO = false);
    }

    record CharInstruction(string Source, int Offset, char Symbol, int Repeat = 1) : Instruction(Source, Offset) {
        public override void Run(Tape tape, ITerminal io) {
            switch (Symbol) {
                case '-': tape.Increment((byte)-Repeat); break;
                case '+': tape.Increment((byte)Repeat); break;
                case '<': tape.Move((ushort)-Repeat); break;
                case '>': tape.Move((ushort)Repeat); break;
                case ',':
                    for (int i = 0; i < Repeat; i++) if (io.Read() is byte b) tape.Write(b);
                    break;
                case '.':
                    for (int i = 0; i < Repeat; i++) io.Write(tape.Read());
                    break;
                default: throw new InvalidProgramException();
            }
        }

        public override void Serialize(StringBuilder builder, bool optimize) {
            for (int i = 0; i < Repeat; i++) {
                char inverse = Symbol switch { '-' => '+', '+' => '-', '<' => '>', '>' => '<', _ => '?' };
                if (builder.Length == 0) {
                    if (!"<>".Contains(Symbol)) builder.Append(Symbol);
                }
                else if (builder[^1] == inverse) --builder.Length;
                else builder.Append(Symbol);
            }
        }
    }

    record Loop(string Source, int Offset, BrainfuckProgram Body) : Instruction(Source, Offset) {
        public int RunCount { get; private set; } = 0;
        public override void Run(Tape tape, ITerminal io) {
            while (tape.Read() != 0) {
                this.RunCount += 1;
                Body.Run(tape, io);
            }
        }

        public override void Serialize(StringBuilder builder, bool usePGO) {
            if (usePGO && RunCount == 0) return;
            builder.Append('[');
            Body.Serialize(builder, usePGO);
            builder.Append(']');
        }
    }

    record Comment(string Source, int Offset, string Message) : Instruction(Source, Offset) {
        public override void Run(Tape tape, ITerminal io) { }

        public override void Serialize(StringBuilder builder, bool usePGO) { }
    }
}
