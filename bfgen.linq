<Query Kind="Program">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
</Query>

void Main() {
	ProgramBuilder builder = new();
    
	int n = builder.Allocate(nameof(n));
	builder.Increment(n, 247);
	int ten = builder.Allocate(nameof(ten));
	builder.Increment(ten, 10);
	
	int newn = builder.Allocate(nameof(newn));
	int range = builder.AllocateRange(3);

	using (builder.Loop(n)) {
		builder.Comment("Pushing range");
		builder.PushRange();
		builder.Comment("Doing divmod");
		builder.DivMod(newn, range, n, ten);
		builder.Comment("Increment 48");
		builder.Increment(range, 48);
		builder.Comment("Move back to n");
		builder.AddAndZero(n, newn);
		builder.Comment("Finishing loop");
	}
	builder.Release(newn);
	
	builder.Comment("Moving to range");
	builder.MoveTo(range);
	builder.Comment("Outputting");
	builder.Do("[.>]");
	builder.NewLine();
	
	builder.ReleaseRange();
	
    builder.Build().Dump();
}

enum CellDisposition {
	Zero,
	NonZero,
	Unknown,
}

class ProgramBuilder : IDisposable {
	const int MemorySize = 256;

	private enum BlockType { Top, Loop, If }
	private record Block(
		BlockType Type, 
		int ControlVariable, 
		// Allocations in the current scope known to be zero at the time of first use.
        // They may need to be zeroed for the next loop iteration if one of them becomes possibly non-zero
        // before the end of the loop.
        List<int> UnzeroedAllocations, 
		CellDisposition[] States);

	private class BlockStack {
		private readonly Stack<Block> ControlBlocks = new();
		
		public BlockStack() {
			ControlBlocks.Push(new(BlockType.Top, -1, new(), new CellDisposition[MemorySize]));
		}

		public void Push(BlockType type, int controlVariable) => ControlBlocks.Push(
			new(type, controlVariable, new(), ControlBlocks.Peek().States.ToArray()));
		
		public Block Pop() {
			Block inner = ControlBlocks.Pop(), outer = ControlBlocks.Peek();
			for (int i = 0; i < MemorySize; i++) {
				outer.States[i] = (outer.States[i], inner.States[i]) switch {
					(var a, var b) when a == b => a,
					_ => CellDisposition.Unknown,
				};
			}
			return inner;
		}
		
		public Block Current => ControlBlocks.Peek();
	}
	BlockStack ControlBlocks = new();
	CellDisposition[] States => ControlBlocks.Current.States;
	
	public bool GenerateComments { get; set; } = true;

	private StringBuilder Program = new();
	private int Head;
    private bool[] Allocated = new bool[MemorySize];
	
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
		if (!GenerateComments) return this;
		if (Program.Length != 0 && Program[Program.Length - 1] != '\n') Do('\n');
		return Do($"`{ comment }`\n");
	}
	
	public ProgramBuilder Loop(int condition) {
		ControlBlocks.Push(BlockType.Loop, condition);
		MoveTo(condition).Do('[');
		States[condition] = CellDisposition.NonZero;
		return this;
	}
	
	public ProgramBuilder EndLoop() {
		var (type, condition, unzeroed, states) = ControlBlocks.Current;
		if (type != BlockType.Loop) throw new ($"Tried to end loop, but was actually ${type}");
		foreach (var uz in unzeroed) {
			if (States[uz] != CellDisposition.Zero) Zero(uz, false);
		}
		MoveTo(condition).Do(']');
		ControlBlocks.Pop();
		States[condition] = CellDisposition.Zero;
		return this;
	}
	
	/// <summary>if (condition) { condition = 0; </summary>
	public ProgramBuilder If(int condition) {
		ControlBlocks.Push(BlockType.If, condition);
		MoveTo(condition).Do('[');
		return this;
	}
	
	public ProgramBuilder EndIf() {
		var (type, condition, unzeroed, states) = ControlBlocks.Current;
		if (type != BlockType.If) throw new ($"Tried to end if, but was actually ${type}");
		MoveTo(condition).Zero(condition, false).Do(']');
		ControlBlocks.Pop();
		States[condition] = CellDisposition.Zero;
		return this;
	}
	
	public void Dispose() {
		switch (ControlBlocks.Current.Type) {
			case BlockType.Loop: EndLoop(); break;
			case BlockType.If: EndIf(); break;
			case BlockType.Top: throw new ("Can't close the top level block");
		}
	}
	
	/// <summary>x=0;</summary>
	public ProgramBuilder Zero(int var, bool ensureZeroedAtCloseOfLoop) {
		if (States[var] != CellDisposition.Zero) {
			using (Loop(var)) Decrement(var);
		}
		else if (ensureZeroedAtCloseOfLoop) {
			if (!ControlBlocks.Current.UnzeroedAllocations.Contains(var))
				ControlBlocks.Current.UnzeroedAllocations.Add(var);
		}
		return this;
	}
	
	public int Allocate(string? debugName = default) {
		int var = Array.IndexOf(Allocated, false);
		if (var >= Range - 1) throw new ("failed to allocate. no memory free.");
		if (debugName is string) Comment($"Allocating { debugName } at { var }");
        Allocated[var] = true;
		Zero(var, true);
        return var;
	}
	
	public void Release(int var) {
		if (!Allocated[var]) throw new ("Can't release what was not yours");
        Allocated[var] = false;
	}
	
	public void Release(params int[] vars) {
        foreach (int var in vars) Release(var);
	}
	
	private int? Range = null;
	public int AllocateRange(int additionalAllocationBuffer = 0) {
		if (Range.HasValue) throw new ("can't allocate multiple ranges");
		int range = Array.LastIndexOf(Allocated, true) + 2 + additionalAllocationBuffer;
		Comment($"Allocated range at { range } leaving buffer of { additionalAllocationBuffer }");
		Range = range;
		return range;
	}
	
	public void ReleaseRange() {
		if (!Range.HasValue) throw new ("no range to release");
		for (int i = Range.Value; i < States.Length; i++) {
			States[i] = CellDisposition.Unknown;
		}
		Range = null;
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
		
	/// <summary>acc += operand; operand = 0;</summary>
	public ProgramBuilder AddAndZero(int acc, int operand) {
		using(Loop(operand)) {
			Decrement(operand);
			Increment(acc);
		}
		return this;
	}
    
    /// <summary>acc -= operand; operand = 0;</summary>
    public ProgramBuilder SubAndZero(int acc, int operand) {
        using (Loop(operand)) {
            Decrement(acc);
            Decrement(operand);
        }
        return this;
    }
    
	/// <summary>target1=target2=source; source=0;</summary>
	public ProgramBuilder MoveTwice(int target1, int target2, int source) {
		Zero(target1, false).Zero(target2, false);
		using (Loop(source)) {
			Decrement(source);
			Increment(target1);
			Increment(target2);
		}
		return this;
	}
		
	/// <summary>x=y;</summary>
	public ProgramBuilder Copy(int target, int source) {
		int temp = Allocate();
		MoveTwice(target, temp, source);
		AddAndZero(source, temp);
		Release(temp);
		return this;
	}
	
	/// <summary>declare variable copying value</summary>
	public int AllocateAndCopy(int var, string? debugName = default) {
		int target = Allocate(debugName);
		Copy(target, var);
		return target;
	}
	
	/// <summary>acc+=operand;</summary>
	public ProgramBuilder Add(int acc, int operand) {
		int temp = AllocateAndCopy(operand);
		AddAndZero(acc, temp);
		Release(temp);
		return this;
	}
	
	/// <summary>target = operand1 * operand2;</summary>
	public ProgramBuilder Mul(int target, int operand1, int operand2) {
		Zero(target, false);
		using (Loop(operand2)) {
			Add(target, operand1);
			Decrement(operand2);
		}
		return this;
	}
	
	/// <summary>target = operand == 0 ? 1 : 0; operand = 0;</summary>
	public ProgramBuilder Not(int target, int operand) {
		Zero(target, false).Increment(target);
		using(If(operand)) {
			Zero(target, false);
		}
		return this;
	}
    
    /// <summary>x = x == y; y = ???;</summary>
    public ProgramBuilder Eq(int x, int y) {
        SubAndZero(y, x);
        Not(x, y);
        return this;
    }
	
	/// <summary>target = numerator/denominator; numerator = 0;</summary>
	public ProgramBuilder Div(int target, int numerator, int denominator) {
		int progress = AllocateAndCopy(denominator, nameof(progress));
		Zero(target, false);
		using (Loop(numerator)) {
			Decrement(numerator);
			Decrement(progress);
			int ztest = Allocate(nameof(ztest));
			int progresscopy = AllocateAndCopy(progress, nameof(progresscopy));
			Not(ztest, progresscopy);
			Release(progresscopy);
			using (If(ztest)) {
				Copy(progress, denominator);
				Increment(target);
			}
			Release(ztest);
		}
		return this;
	}
    
    /// <summary>target = numerator % divisor; numerator = 0;</summary>
    public ProgramBuilder Mod(int target, int numerator, int divisor) {
        Zero(target, false);
        using (Loop(numerator)) {
            Decrement(numerator);
            Increment(target);
            
            int targetCopy = AllocateAndCopy(target, nameof(targetCopy));
            int divisorCopy = AllocateAndCopy(divisor, nameof(divisorCopy));
            Eq(targetCopy, divisorCopy);
            using (If(targetCopy)) {
                Zero(target, false);
            }
            Release(targetCopy, divisorCopy);
        }
        return this;
    }
    
    /// <summary>div = numerator / divisor; mod = numerator % divisor; numerator = 0;</summary>
    public ProgramBuilder DivMod(int div, int mod, int numerator, int divisor) {
        Zero(div, false).Zero(mod, false);
        using (Loop(numerator)) {
            Decrement(numerator);
            Increment(mod);
            
            int modCopy = AllocateAndCopy(mod);
            int divisorCopy = AllocateAndCopy(divisor);
            Eq(modCopy, divisorCopy);
            using (If(modCopy)) {
                Zero(mod, false);
                Increment(div);
            }
            Release(modCopy, divisorCopy);
        }
        return this;
    }
    
    public ProgramBuilder NewLine() {
        int nl = Allocate();
        Increment(nl, 10);
        Do('.');
        Release(nl);
        return this;
    }
    
    public ProgramBuilder PrintDigit(int var) {
        Increment(var, 48);
        Do('.');
        Decrement(var, 48);
        return this;
    }
    
	/// <summary>insert a 0 at the head of the current range</summary>
    public ProgramBuilder PushRange() {
		MoveTo(Range.Value);
		
		Do("[>]<"); // go to last element
		Do("[[->+<]<]>"); // they all rolled over
		
		return this;
	}
}

