<Query Kind="Program">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

const string SourceKey = "source", TapeNameKey = "names", InputKey = "input";
void Main() {
    var run = Util.KeepRunning();
    
	Util.HtmlHead.AddStyles(".current{background:#e628;} body{font-size:150%;}");
	Util.HtmlHead.AddScript(@"
		function scrollDebugger() {
			setTimeout(() => {
				const currents = document.querySelectorAll('span.current');
				const last = currents[currents.length - 1];
				if (last) last.scrollIntoView(false);
			}, 100);
		}
		window.addEventListener('keydown', ev => {
			const idmap = {
				'KeyQ': 'load',
				'KeyW': 'step',
				'KeyE': 'continue',
				'KeyR': 'step-out',
				'KeyT': 'run',
			};
			const id = idmap[ev.code];
			if (id) document.getElementById(id).click();
		});");
	
    var source = new TextArea(Util.LoadString(SourceKey) ?? ""){ Rows = 6 };
	var input = new TextArea(Util.LoadString(InputKey) ?? "") { Cols = 12 };
	Util.HorizontalRun("Source,Input", source, input).Dump();
    var stepView = new DumpContainer() {
		Style = "white-space: pre-wrap; max-height: 50vh; overflow-y: scroll;",
	}.Dump("Instruction Head");
    var tapeView = new DumpContainer().Dump("Tape");
    var output = new DumpContainer().Dump("Output");
	
    Program prog = Parse("");
    Tape tape = new();
    BuilderIO io = new();
    
    void Update() {
        string progDisplay = prog.GetDisplayString(true);
		progDisplay = Regex.Replace(progDisplay, @"\^(\S+)", "<span class=current>$1</span>");
        stepView.Content = Util.RawHtml(progDisplay);
        tapeView.Content = tape.ToString();
        output.Content = io.Output;
		Util.InvokeScript(false, "scrollDebugger");
    }
    void Load(Button? _) {
        Util.SaveString(SourceKey, source.Text);
		Util.SaveString(InputKey, input.Text);
        tape = new();
        prog = Parse(source.Text);
        io.Reset(input.Text.Replace("\r", ""));
        Update();
    }
    void Step(Button? _) {
        prog.Step(tape, io);
        Update();
    }
	void Continue(Button? _) {
		prog.Continue(tape, io);
		Update();
	}
    void StepOut(Button? _) {
        prog.StepOut(tape, io);
        Update();
    }
    void Run(Button? _) {
        prog.Run(tape, io);
        Update();
    }
    
    Util.HorizontalRun(withGaps: true, 
        new Button("Load (Q)", Load) { HtmlElement = { ID = "load" } }, 
        new Button("Step (W)", Step) { HtmlElement = { ID = "step" } }, 
		new Button("Continue (E)", Continue) { HtmlElement = { ID = "continue" } },
        new Button("Step Out (R)", StepOut) { HtmlElement = { ID = "step-out" } },
        new Button("Run (T)", Run) { HtmlElement = { ID = "run" } }
    ).Dump();
    
    Load(default);
}

interface ITerminal {
    void Write(byte val);
    byte Read();
}

class BuilderIO : ITerminal {
	private StringReader Input = new("");
    private StringBuilder Buffer = new();
    
    public string Output => Buffer.ToString();
    
    public void Reset(string input) {
		Buffer.Clear();
		Input = new(input);
	}
	
    public byte Read() => (byte)Math.Max(0, Input.Read());

    public void Write(byte val) => Buffer.Append((char)val);
}

class Tape {
    public override string ToString() {
        StringBuilder result = new();
		const int CellWidth = 8;
		foreach (var t in TapeLabels) {
			result.Append(t.Length < CellWidth ? t.PadRight(CellWidth) : t[..CellWidth]).Append(' ');
		}
		result.AppendLine();
        for (ushort i = LeftFrontier; i != RightFrontier + 1; i++) {
            result.AppendFormat("{0:X2}:{1:X2}({2}) ", 
				i - InitialHead & 0xff,
				Memory[i], 
				Memory[i] >= 32 && Memory[i] < 127 ? (char)Memory[i] : '?');
        }
        result.AppendLine();
        result.AppendLine("^".PadLeft((ushort)(Head - LeftFrontier) * 6 + 1));
        return result.ToString();
    }

	private List<string> TapeLabels = new();
    private byte[] Memory = new byte[0x10000];
	private const ushort InitialHead = 0x8000;
    private ushort Head = 0x8000;
    private ushort LeftFrontier = 0x8000;
    private ushort RightFrontier = 0x8000;

    public byte Read() => Memory[Head];
	public void Write(byte v) => Memory[Head] = v;
    public byte Increment(byte by) => Memory[Head] += by;
    public void Move(ushort by) {
        Head += by;
		LeftFrontier = Math.Min(LeftFrontier, Head);
		RightFrontier = Math.Max(RightFrontier, Head);
    }

	public void SetName(int v, string name) {
		while (TapeLabels.Count <= v) TapeLabels.Add("");
		TapeLabels[v] = name;
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
        for (; ip < Instructions.Count; ip++) {
            Instructions[ip].Run(tape, io);
        }
		ip = 0;
    }

    // finished?
    public bool Step(Tape tape, ITerminal io) {
        if (ip >= Instructions.Count) return true;
        if (Instructions[ip].Step(tape, io)) ip++;
        return ip >= Instructions.Count;
    }

	// stepped out of a loop?
    public bool StepOut(Tape tape, ITerminal io) {
        if (ip >= Instructions.Count) return false;
        
		if (Instructions[ip] is Loop { Inside: true } loop) {
            loop.StepOut(tape, io);
            if (!loop.Inside) ip++;
			return true;
        }
        
		for (; ip < Instructions.Count; ip++) {
            Instructions[ip].Run(tape, io);
        }
		return false;
    }
	
	// continued a loop?
    public bool Continue(Tape tape, ITerminal io) {
        if (ip >= Instructions.Count) return false;
        
		if (Instructions[ip] is Loop { Inside: true } loop) {
            loop.Continue(tape, io);
			return true;
        }
        
		for (; ip < Instructions.Count; ip++) {
            Instructions[ip].Run(tape, io);
        }
		return false;
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
        if (isCurrent) result = '^' + result;
        return result;
    }

    public override void Run(Tape tape, ITerminal io) => Step(tape, io);

    public override bool Step(Tape tape, ITerminal io) {
        switch (Symbol) {
            case '-': tape.Increment((byte)-Repeat); return true;
            case '+': tape.Increment((byte)Repeat); return true;
            case '<': tape.Move((ushort)-Repeat); return true;
            case '>': tape.Move((ushort)Repeat); return true;
            case ',': 
				for (int i = 0; i < Repeat; i++) tape.Write(io.Read());
				return true;
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
        return (isCurrent)
            ? $"^[ { Body.GetDisplayString(isCurrent & Inside) } ]"
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
        if (!Body.StepOut(tape, io)) {
			this.Run(tape, io);
			Inside = false;
			Body.JumpToStart();
		}
    }

	public void Continue(Tape tape, ITerminal io) {
		if (!Body.Continue(tape, io)) {
			this.Body.Run(tape, io);
			Inside = false;
			Body.JumpToStart();
		}
	}
}

record Comment(string Source, int Offset, string Message) : Instruction(Source, Offset) {
	public override string GetDisplayString(bool isCurrent)
		=> isCurrent 
			? $"\n^`{ Message }`"
			: $"\n`{ Message }`";

	public override void Run(Tape tape, ITerminal io) => Step(tape, io);

	public override bool Step(Tape tape, ITerminal io) {
		if (Regex.Match(Message, @"\[(\d+)\]: ?(.*)$") is { Success: true } m) {
			tape.SetName(int.Parse(m.Groups[1].Value), m.Groups[2].Value);
		}
		return true;
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
    while (sp < prog.Length) {
        if (prog[sp] == '[') {
            ++sp; // [
            instr.Add(new Loop(prog, sp - 1, Parse(prog, ref sp)));
            if (sp >= prog.Length) throw new ("Expected ']' at EOF");
            if (prog[sp] != ']') throw new ($"Expected ']' at {sp}");
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
