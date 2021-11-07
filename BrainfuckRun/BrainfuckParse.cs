using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfuckRun {
    public class BrainfuckParse {
        public static BrainfuckProgram Parse(string prog) {
            int ip = 0;
            var result = Parse(prog, ref ip);
            if (ip != prog.Length) throw new($"Problem at {ip}");
            return result;
        }

        public static BrainfuckProgram Parse(string prog, ref int sp) {
            List<Instruction> instr = new();
            while (sp < prog.Length) {
                if (prog[sp] == '[') {
                    ++sp; // [
                    instr.Add(new Loop(prog, sp - 1, Parse(prog, ref sp)));
                    if (sp >= prog.Length) throw new("Expected ']' at EOF");
                    if (prog[sp] != ']') throw new($"Expected ']' at {sp}");
                    ++sp; // ]
                }
                else if ("-+<>.,".Contains(prog[sp])) {
                    char symbol = prog[sp];
                    int end = sp;
                    if (sp + 1 < prog.Length && char.IsDigit(prog[sp + 1])) {
                        // repetition modifier
                        int reps = 0;
                        while (sp + 1 < prog.Length && char.IsDigit(prog[end + 1])) {
                            reps = reps * 10 + prog[++end] - '0';
                        }
                        instr.Add(new CharInstruction(prog, sp, symbol, reps));
                    }
                    else {
                        // run length
                        while (end < prog.Length && prog[end] == prog[sp]) end++;
                        instr.Add(new CharInstruction(prog, sp, symbol, end - sp));
                    }
                    sp = end;
                }
                else if (prog[sp] == '`') {
                    int end = prog.IndexOf('`', ++sp);
                    if (end == -1) continue;
                    instr.Add(new Comment(prog, sp - 1, prog[sp..end].Trim()));
                    sp = end + 1;
                }
                else if (prog[sp] == ']') break;
                else ++sp;
            }
            return new(instr);
        }
    }
}
