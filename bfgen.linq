<Query Kind="Program">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
</Query>

void Main() {
	ProgramBuilder b = new();

	int x = b.Allocate();
	b.Increment(x, 20);
	int y = b.Allocate();
	b.Increment(y, 3);
	int div = b.Allocate();
	b.Div(div, x, y);
	
	//int n = b.Allocate();
	//b.Increment(n, 3);
	//using (b.StartLoop(n)) {
	//	b.Decrement(n);
	//	int t = b.Allocate();
	//	b.Increment(t, 48).Do('.').Decrement(t, 47);
	//}
	
	b.Build().Dump();
}

enum CellDisposition {
	Zero,
	NonZero,
	Unknown,
}

class ProgramBuilder : IDisposable {
	const int MemorySize = 256;

	private enum BlockType { Loop, If }
	private record Block(BlockType Type, int ControlVariable, List<int> UnzeroedAllocations);

	private StringBuilder Program = new();
	private int Head;
	private readonly Stack<Block> ControlBlocks = new();
    private bool[] Allocated = new bool[MemorySize];
	private CellDisposition[] States = new CellDisposition[MemorySize];
	private bool[] Locked = new bool[MemorySize];
	
	public string Build() => Program.ToString();
	
	public ProgramBuilder MoveTo(int var) {
		if (var > Head) Program.Append(new string('>', var - Head));
		else Program.Append(new string('<', Head - var));
		Head = var;
		return this;
	}
	
	public ProgramBuilder Do(char c) {
		Program.Append(c);
		return this;
	}
	
	public ProgramBuilder Do(string c) {
		Program.Append(c);
		return this;
	}
	
	public ProgramBuilder Comment(string comment) {
		if (Program[Program.Length - 1] != '\n') Do('\n');
		return Do($"`{ comment }`\n");
	}
	
	public ProgramBuilder StartLoop(int condition) {
		ControlBlocks.Push(new (BlockType.Loop, condition, new()));
		MoveTo(condition).Do('[');
		States[condition] = CellDisposition.NonZero;
		return this;
	}
	
	public ProgramBuilder EndLoop() {
		var (type, condition, unzeroed) = ControlBlocks.Pop();
		if (type != BlockType.Loop) throw new ($"Tried to end loop, but was actually ${type}");
		foreach (var uz in unzeroed) {
			if (States[uz] != CellDisposition.Zero) Zero(uz);
		}
		MoveTo(condition).Do(']');
		States[condition] = CellDisposition.Zero;
		return this;
	}
	
	/// <summary>if (condition) { condition = 0; </summary>
	public ProgramBuilder StartIf(int condition) {
		ControlBlocks.Push(new (BlockType.If, condition, new()));
		MoveTo(condition).Do('[');
		Locked[condition] = false;
		return this;
	}
	
	public ProgramBuilder EndIf() {
		var (type, condition, unzeroed) = ControlBlocks.Pop();
		if (type != BlockType.If) throw new ($"Tried to end if, but was actually ${type}");
		MoveTo(condition).Zero(condition).Do(']');
		States[condition] = CellDisposition.Zero;
		return this;
	}
	
	public void Dispose() {
		switch (ControlBlocks.Peek().Type) {
			case BlockType.Loop: EndLoop(); break;
			case BlockType.If: EndIf(); break;
		}
	}
	
	/// <summary>x=0;</summary>
	public ProgramBuilder Zero(int var) {
		if (States[var] != CellDisposition.Zero) {
			using (StartLoop(var)) Decrement(var);
		}
		else if (ControlBlocks.Count > 0) {
			ControlBlocks.Peek().UnzeroedAllocations.Add(var);
		}
		return this;
	}
	
	public int Allocate(string? debugName = default) {
		int var = Array.IndexOf(Allocated, false);
		if (debugName is string) Comment($"Allocating { debugName } at { var }");
        Allocated[var] = true;
		Zero(var);
        return var;
	}
	
	public void Release(int var) {
		if (!Allocated[var]) throw new ("Can't release what was not yours");
        Allocated[var] = false;
	}
	
	public ProgramBuilder Increment(int x, int times = 1) {
		times = times & 0xff;
		if (times == 0) return this;
		MoveTo(x).Do(new string('+', times));
		States[x] = States[x] == CellDisposition.Zero 
			? CellDisposition.NonZero 
			: CellDisposition.Unknown;
		return this;
	}

	public ProgramBuilder Decrement(int x, int times = 1) {
		times = times & 0xff;
		if (times == 0) return this;
		MoveTo(x).Do(new string('-', times));
		States[x] = States[x] == CellDisposition.Zero 
			? CellDisposition.NonZero 
			: CellDisposition.Unknown;
		return this;
	}
		
	/// <summary>target+=donor; donor=0;</summary>
	public ProgramBuilder Donate(int target, int donor) {
		using(StartLoop(donor)) {
			Decrement(donor);
			Increment(target);
		}
		return this;
	}
		
	/// <summary>target1=target2=source; source=0;</summary>
	public ProgramBuilder MoveTwice(int target1, int target2, int source) {
		Zero(target1).Zero(target2);
		using (StartLoop(source)) {
			Decrement(source);
			Increment(target1);
			Increment(target2);
		}
		return this;
	}
		
	/// <summary>x=y;</summary>
	public ProgramBuilder Copy(int target, int source) {
		int temp=Allocate();
		MoveTwice(target, temp, source);
		Donate(source, temp);
		Release(temp);
		return this;
	}
	
	public int AllocateAndCopy(int var, string? debugName = default) {
		int target = Allocate(debugName);
		Copy(target, var);
		return target;
	}
	
	/// <summary>acc+=operand;</summary>
	public ProgramBuilder Add(int acc, int operand) {
		int temp = AllocateAndCopy(operand);
		Donate(acc, temp);
		Release(temp);
		return this;
	}
	
	/// <summary>target = operand1 * operand2;</summary>
	public ProgramBuilder Mul(int target, int operand1, int operand2) {
		Zero(target);
		using (StartLoop(operand2)) {
			Add(target, operand1);
			Decrement(operand2);
		}
		return this;
	}
	
	/// <summary>target = operand == 0 ? 1 : 0; operand = 0;</summary>
	public ProgramBuilder Not(int target, int operand) {
		Zero(target).Increment(target);
		using(StartIf(operand)) {
			Zero(target);
		}
		return this;
	}
	
	/// <summary>target = numerator/denominator; numerator = 0;</summary>
	public ProgramBuilder Div(int target, int numerator, int denominator) {
		Comment($"div inputs - target: { target }, numerator: { numerator } denominator: {denominator }");
		int progress = AllocateAndCopy(denominator, nameof(denominator));
		Zero(target);
		Comment("starting main div loop");
		using (StartLoop(numerator)) {
			Comment("--numerator");
			Decrement(numerator);
			Comment("--progress");
			Decrement(progress);
			int ztest = Allocate(nameof(ztest));
			int progresscopy = AllocateAndCopy(progress, nameof(progresscopy));
			Comment("ztest = !progresscopy");
			Not(ztest, progresscopy);
			Release(progresscopy);
			Comment("if ztest");
			using (StartIf(ztest)) {
				Comment("copy progress = denominator");
				Copy(progress, denominator);
				Comment("++target");
				Increment(target);
			}
			Release(ztest);
		}
		return this;
	}
}

