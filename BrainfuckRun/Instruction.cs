using System;
using System.Collections.Generic;

namespace BrainfuckRun {
    record BrainfuckProgram(IList<Instruction> Instructions) {
        private int ip = 0;

        public void Run(Tape tape, ITerminal io) {
            for (; ip < Instructions.Count; ip++) {
                Instructions[ip].Run(tape, io);
            }
            ip = 0;
        }
    }

    abstract record Instruction(string Source, int Offset) {
        public abstract void Run(Tape tape, ITerminal io);
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
    }

    record Loop(string Source, int Offset, BrainfuckProgram Body) : Instruction(Source, Offset) {
        public override void Run(Tape tape, ITerminal io) {
            while (tape.Read() != 0) Body.Run(tape, io);
        }
    }

    record Comment(string Source, int Offset, string Message) : Instruction(Source, Offset) {
        public override void Run(Tape tape, ITerminal io) { }
    }
}
