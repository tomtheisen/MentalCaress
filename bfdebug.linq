<Query Kind="Program">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

const string SourceKey = "source";
void Main() {
    //var run = Util.KeepRunning();
    
    var source = new TextArea(Util.LoadString(SourceKey) ?? ""){ Rows = 6 }.Dump("Source");
    var stepView = new DumpContainer().Dump("Instruction Head");
    var tapeView = new DumpContainer().Dump("Tape");
    var output = new DumpContainer().Dump("Output");

    Program prog = Parse("");
    Tape tape = new();
    BuilderIO io = new();
    
    void Update() {
        string progDisplay = prog.GetDisplayString(true);
        int caret = progDisplay.IndexOf('*');
        stepView.Content = caret < 0 ? progDisplay
            : progDisplay.Remove(caret, 1) + "\n" + "^".PadLeft(caret + 1);
        tapeView.Content = tape.ToString();
        output.Content = io.Output;
    }
    void Load(Button? _) {
        Util.SaveString(SourceKey, source.Text);
        tape = new();
        prog = Parse(source.Text);
        io.Reset();
        Update();
    }
    void Step(Button? _) {
        prog.Step(tape, io);
        Update();
    }
    void Run(Button? _) {
        prog.Run(tape, io);
        Update();
    }
    void StepOut(Button? _) {
        prog.StepOut(tape, io);
        Update();
    }
    
    Util.HorizontalRun(withGaps: true, 
        new Button("Load", Load), 
        new Button("Step", Step), 
        new Button("Step Out", StepOut),
        new Button("Run", Run)
    ).Dump();
    
    Load(default);
}

interface ITerminal {
    void Write(byte val);
    byte Read();
}

class BuilderIO : ITerminal {
    private StringBuilder Buffer = new();
    
    public string Output => Buffer.ToString();
    
    public void Reset() => Buffer.Clear();

    public byte Read() => (byte)System.Console.Read();

    public void Write(byte val) => Buffer.Append((char)val);
}

class Tape {
    public override string ToString() {
        StringBuilder result = new();
        for (ushort i = LeftFrontier; i != RightFrontier + 1; i++) {
            result.AppendFormat("{0:X2}({1}) ", Memory[i], Memory[i] >= 32 && Memory[i] < 127 ? (char)Memory[i] : '?');
        }
        result.AppendLine();
        result.AppendLine("^".PadLeft((ushort)(Head - LeftFrontier) * 6 + 1));
        return result.ToString();
    }

    private byte[] Memory = new byte[0x10000];
    private ushort Head = 0x8000;
    private ushort LeftFrontier = 0x8000;
    private ushort RightFrontier = 0x8000;

    public byte Read() => Memory[Head];
    public byte Increment(byte by) => Memory[Head] += by;
    public void Move(ushort by) {
        Head += by;
		LeftFrontier = Math.Min(LeftFrontier, Head);
		RightFrontier = Math.Max(RightFrontier, Head);
    }
}

record Program(IList<Instruction> Instructions) {
    public string GetDisplayString(bool isCurrent) {
        return string.Join(' ', Instructions.Select(
            (instr, idx) => instr.GetDisplayString(isCurrent && idx == ip)));
    }
    public override string ToString() => this.GetDisplayString(isCurrent: false);

    private int ip = 0;

    public void Run(Tape tape, ITerminal io) {
        for (ip = 0; ip < Instructions.Count; ip++) {
            Instructions[ip].Run(tape, io);
        }
    }

    // finished?
    public bool Step(Tape tape, ITerminal io) {
        if (ip >= Instructions.Count) return true;
        if (Instructions[ip].Step(tape, io)) ip++;
        return ip >= Instructions.Count;
    }

    public void StepOut(Tape tape, ITerminal io) {
        if (ip >= Instructions.Count) return;
        if (Instructions[ip] is Loop { Inside: true } loop) {
            loop.StepOut(tape, io);
            ip++;
        }
        else for (; ip < Instructions.Count; ip++) {
            Instructions[ip].Run(tape, io);
        }
    }

    public void JumpToStart() => ip = 0;
}

abstract record Instruction(string Source, int Offset) {
    public abstract void Run(Tape tape, ITerminal io);
    public abstract bool Step(Tape tape, ITerminal io); // finished?
    public abstract string GetDisplayString(bool isCurrent);
    public override string ToString() => this.GetDisplayString(isCurrent: false);
}

record CharInstruction(string Source, int Offset, char Symbol, int Repeat = 1) : Instruction(Source, Offset)  {
    public override string GetDisplayString(bool isCurrent) {
        var result = Repeat == 1 
            ? $"{Symbol}" 
            : $"{Symbol}{{{Repeat}}}";
        if (isCurrent) result = '*' + result;
        return result;
    }

    public override void Run(Tape tape, ITerminal io) => Step(tape, io);

    public override bool Step(Tape tape, ITerminal io) {
        switch (Symbol) {
            case '-': tape.Increment((byte)-Repeat); return true;
            case '+': tape.Increment((byte)Repeat); return true;
            case '<': tape.Move((ushort)-Repeat); return true;
            case '>': tape.Move((ushort)Repeat); return true;
            case ',': throw new NotImplementedException();
            case '.': 
                for (int i = 0; i < Repeat; i++) io.Write(tape.Read());
                return true;
            default: throw new InvalidProgramException();
        }
    }
}

record Loop(string Source, int Offset, Program Body) : Instruction(Source, Offset) {
    public bool Inside { get; private set; } = false;

    public override string GetDisplayString(bool isCurrent) {
        return (isCurrent && !Inside)
            ? $"*[ { Body.GetDisplayString(false) } ]"
            : $"[ { Body.GetDisplayString(isCurrent) } ]";
    }

    public override void Run(Tape tape, ITerminal io) {
        while (tape.Read() != 0) Body.Run(tape, io);
    }

    public override bool Step(Tape tape, ITerminal io) {
        if (!Inside) {
            if (tape.Read() == 0) return true;
            Inside = true;
            return false;
        }
        else {
            if (Body.Step(tape, io)) {
                Inside = false;
                Body.JumpToStart();
            }
            else Inside = true;
            return false;
        }
    }

    public void StepOut(Tape tape, ITerminal io) {
        Body.StepOut(tape, io);
        this.Run(tape, io);
    }
}

Program Parse(string prog) {
    int ip = 0;
    var result = Parse(prog, ref ip);
    if (ip != prog.Length) throw new($"Problem at {ip}");
    return result;
}

Program Parse(string prog, ref int sp) {
    List<Instruction> instr = new();
    for (; sp < prog.Length; ) {
        if (prog[sp] == '[') {
            ++sp; // [
            instr.Add(new Loop(prog, sp - 1, Parse(prog, ref sp)));
            if (sp >= prog.Length) throw new ("Expected ']' at EOF");
            if (prog[sp] != ']') throw new ($"Expected ']' at {sp}");
            ++sp; // ]
        }
        else if ("-+<>.,".Contains(prog[sp])) {
            char symbol = prog[sp];
            int re = sp;
            if (sp + 1 < prog.Length && char.IsDigit(prog[sp + 1])) {
                // repetition modifier
                int reps = 0;
                while (sp + 1 < prog.Length && char.IsDigit(prog[re + 1])) {
                    reps = reps * 10 + prog[++re] - '0';
                }
                instr.Add(new CharInstruction(prog, sp, symbol, reps));
            }
            else {
                // run length
                while (re < prog.Length && prog[re] == prog[sp]) re++;
                instr.Add(new CharInstruction(prog, sp, symbol, re - sp));
            }
            sp = re;
        }
        else if (prog[sp] == ']') break;
        else ++sp;
    }
    return new(instr);
}
