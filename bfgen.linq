<Query Kind="Program">
  <NuGetReference Version="2.3.1">Sprache</NuGetReference>
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
  <Namespace>Sprache</Namespace>
</Query>

void Main() {
    ProgramBuilder builder = new() { GenerateComments = true };
	
	ScriptParser.Program.Parse(@"
		var x = 0
		loop x {
			x = x + 47
			write x
			x = x - 48
		}
	").Dump();
	
	/* all primes
	builder.Allocate(out int p, 1, nameof(p));
	using (builder.Loop(p)) {
		builder.AllocateAndCopy(out int d, p, nameof(d));
		builder.Decrement(d, 1);
		builder.Increment(p);
	
		builder.Allocate(out int divisors, 0, nameof(divisors));

		builder.Comment("divisor test loop");
		using (builder.Loop(d)) {
			builder.Increment(d, 1);
			builder.AllocateAndCopy(out int _p, p, nameof(_p));

			builder.Allocate(out int remainder, 0, nameof(remainder));
			builder.Mod(remainder, _p, d);
			using (builder.IfNot(remainder)) {
				builder.Increment(divisors);
			}
			
			builder.Decrement(d, 2);
		}
		
		builder.Comment("output condition");
		using (builder.IfNot(divisors)) {
			builder.AllocateAndCopy(out int _p, p, nameof(_p));
			builder.WriteNumber(_p).NewLine();
		}
		builder.Comment("end candidate loop");
	}
	//*/
	
	/* leap years
	builder.Allocate(out int a, 1, nameof(a));
	builder.Allocate(out int b, 8, nameof(b));
	builder.Allocate(out int c, 0, nameof(c));
	builder.Allocate(out int d, 0, nameof(d));
	builder.Allocate(out int ten, 10, nameof(ten));
	builder.Allocate(out int div, 0, nameof(div));
	builder.Allocate(out int mod, 0, nameof(mod));
	builder.Allocate(out int century, 0, nameof(century));
	builder.Allocate(out int quad, 2, nameof(quad));
	builder.Allocate(out int i, 151, nameof(i));
	
	using (builder.Loop(i)) {
		builder.Decrement(i);
		
		builder.AllocateAndCopy(out int _century, century, nameof(_century));
		builder.Allocate(out int doshow, 0, nameof(doshow));
		builder.Allocate(out int not, 1, nameof(not));
		builder.Comment("testing century");
		using (builder.If(_century)) {
			builder.Decrement(century);
			builder.Decrement(not);
			builder.Increment(doshow);
		}
		using (builder.If(not)) {
			builder.Increment(century, 24);
			
			builder.AllocateAndCopy(out int _quad, quad, nameof(_quad));
			builder.Allocate(out int notquad, 0, nameof(notquad));
			builder.Not(notquad, _quad);
			using (builder.If(notquad)) {
				builder.Increment(quad, 4);
				builder.Increment(doshow);
			}
			builder.Release(_quad, notquad);
			
			builder.Decrement(quad);
		}
		builder.Release(_century, not);
		
		
		using (builder.Loop(doshow)) {
			builder.Decrement(doshow);
			builder.WriteDigit(a).WriteDigit(b).WriteDigit(c).WriteDigit(d).NewLine();
		}
		builder.Release(doshow);
		
		builder.Increment(d, 4);
		builder.DivMod(div, mod, d, ten).AddAndZero(d, mod).AddAndZero(c, div);
		builder.DivMod(div, mod, c, ten).AddAndZero(c, mod).AddAndZero(b, div);
		builder.DivMod(div, mod, b, ten).AddAndZero(b, mod).AddAndZero(a, div);
	}
	//*/	
	
    /*
	builder.AllocateAndReadNumber(out int n);
    
    builder.Allocate(out int factor, 0, nameof(factor));
    builder.Increment(factor, 1);
    
    builder.Decrement(n);
    using (builder.Loop(n)) {
        builder.Increment(n).Increment(factor);
        
        builder.AllocateAndCopy(out int ncopy, n, nameof(ncopy));
        builder.Allocate(out int div, 0, nameof(div));
        builder.Allocate(out int mod, 0, nameof(mod));
        builder.DivMod(div, mod, ncopy, factor);
        builder.Release(ncopy);
        
        builder.Allocate(out int divides, 0, nameof(divides));
        builder.Not(divides, mod);
        using(builder.If(divides)) {
            builder.Comment("Found divisor");
            builder.AllocateAndCopy(out int fcopy, factor, nameof(fcopy));
            builder.WriteNumber(fcopy).Release(fcopy).NewLine();
            builder.Zero(n).AddAndZero(n, div);
            builder.Decrement(factor);
        }
        builder.Release(divides, div, mod);
        
        builder.Decrement(n);
    }
    builder.WriteNumber(n).NewLine();
    //*/
    builder.Build().Dump();
}

abstract record CellDisposition();
record KnownValue(byte Value) : CellDisposition;
record UnknownValue() : CellDisposition;

namespace AST {
	public record Value();
	public record Identifier(string name) : Value;
	public record NumberLiteral(byte value) : Value;

	public record Statement();
	public record Declaration(Identifier Id, Value Value) : Statement;
	public record OperateAssign(Identifier Target, Value A, char Operator, Value B) : Statement;
	public record Copy(Identifier Target, Value Source) : Statement;
	
	public record Operation(string Action, Identifier Id) : Statement;
	
	public enum BlockType { Loop, If, IfNot }
	public record Block(BlockType Type, Identifier Control, Statement[] Body) : Statement;
}

static class ScriptParser {
	static Parser<T> AnyOf<T>(params Parser<T>[] parsers) => parsers.Aggregate(Parse.Or);
	
	static Parser<string> AnyOf(params string[] s) 
		=> s.Select(e => Parse.String(e).Text()).Aggregate(Parse.Or);

	static Parser<string> Indent => Parse.Chars(' ', '\t').Many().Text();
	
	static IResult<string> EOF(IInput input) => input.AtEnd 
		? Result.Success("", input) 
		: Result.Failure<string>(input, "not EOF", Enumerable.Empty<string>());
		
	static Parser<string> LineEnd => AnyOf(Parse.String("\n").Text(), Parse.String("\r\n").Text(), EOF);
	
	static Parser<string> Terminator => 
		from w in Parse.Chars(' ', '\t').Many()
		from nl in LineEnd
		select "";
		
	static Parser<AST.Identifier> Identifier => Parse.LetterOrDigit
		.AtLeastOnce()
		.Contained(Parse.Char(' ').Many(), Parse.Char(' ').Many())
		.Text()
		.Select(lod => new AST.Identifier(lod));
		
	static Parser<AST.NumberLiteral> Literal => 
		from digits in Parse.Number.Contained(Parse.Char(' ').Many(), Parse.Char(' ').Many())
		let val = int.Parse(digits)
		where val <= byte.MaxValue
		select new AST.NumberLiteral((byte)val);
		
	static Parser<AST.Value> Value => AnyOf<AST.Value>(Literal, Identifier);
	
	public static Parser<AST.Declaration> Declaration =>
		from v in Parse.String("var")
		from id in Identifier
		from eq in Parse.Char('=')
		from val in Value
		select new AST.Declaration(id, val);
		
	public static Parser<AST.OperateAssign> OperateAssign =>
		from target in Identifier
		from eq in Parse.Char('=')
		from a in Value
		from op in Parse.Chars("-+/*%")
		from b in Value
		select new AST.OperateAssign(target, a, op, b);
		
	public static Parser<AST.Copy> Copy =>
		from target in Identifier
		from eq in Parse.Char('=')
		from source in Value
		select new AST.Copy(target, source);
		
	public static Parser<AST.Operation> Action =>
		from action in AnyOf("read", "write", "writenum")
		from s1 in Parse.Char(' ').AtLeastOnce()
		from id in Identifier
		select new AST.Operation(action, id);
	
	public static Parser<AST.BlockType> BlockType 
		=> AnyOf(
			Parse.String("if").Select(_ => AST.BlockType.If),
			Parse.String("ifnot").Select(_ => AST.BlockType.IfNot),
			Parse.String("loop").Select(_ => AST.BlockType.Loop)
		);
		
	public static Parser<AST.Block> Block => 
		from type in BlockType
		from control in Identifier
		from open in Parse.Char('{').Token()
		from body in StatementList
		from close in Parse.Char('}').Token()
		select new AST.Block(type, control, body.ToArray());
	
	public static Parser<AST.Statement> Statement => 
		from s1 in Indent
		from statement in AnyOf<AST.Statement>(Declaration, OperateAssign, Copy, Action, Block)
		from s2 in Terminator
		select statement;
		
	public static Parser<string> BlankLines =>
		Parse.Chars(" \t").Many().Then(_ => LineEnd).Many().Select(_ => "");
		
	public static Parser<IEnumerable<AST.Statement>> StatementList =>
		Statement.Contained(BlankLines, BlankLines).Many();
		
	public static Parser<IEnumerable<AST.Statement>> Program =>
		StatementList.End();

}

class ProgramBuilder : IDisposable {
    const int MemorySize = 256;

    private enum BlockType { Top, Loop, If }
    private record Block(BlockType Type, int ControlVariable, List<int> LocalVariables);

    private class BlockStack {
        private readonly Stack<Block> ControlBlocks = new();
        
        public BlockStack() {
            ControlBlocks.Push(new(BlockType.Top, -1, new()));
        }

        public void Push(BlockType type, int controlVariable) {
            ControlBlocks.Push(new(type, controlVariable, new()));
        }
        
        public Block Pop() => ControlBlocks.Pop();
        
        public Block Current => ControlBlocks.Peek();
		
		public int Depth => ControlBlocks.Count;
    }
    BlockStack ControlBlocks = new();
    
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
    
    public ProgramBuilder Comment(string comment, bool showStack = true) {
        if (!GenerateComments) return this;
        if (Program.Length != 0 && Program[Program.Length - 1] != '\n') Do('\n');
        if (showStack) {
            var frames = new StackTrace(1).GetFrames();
            var stackNames = frames
                .TakeWhile(f => f.GetMethod()?.DeclaringType == typeof(ProgramBuilder))
                .Select(f => f.GetMethod()?.Name + ":")
                .Reverse()
                .ToList();
            comment = string.Concat(stackNames) + comment;
        }
        return Do($"`{ comment }`\n");
    }
    
    public ProgramBuilder Loop(int condition) {
        ControlBlocks.Push(BlockType.Loop, condition);
        MoveTo(condition).Do('[');
        return this;
    }
    
    public ProgramBuilder EndLoop() {
        var (type, condition, locals) = ControlBlocks.Current;
        if (type != BlockType.Loop) throw new ($"Tried to end loop, but was actually ${type}");
		foreach (int var in locals.ToList()) Release(var);
        MoveTo(condition).Do(']');
        ControlBlocks.Pop();
        return this;
    }
    
    /// <summary>if (condition) { condition = 0; </summary>
    public ProgramBuilder If(int condition) {
        ControlBlocks.Push(BlockType.If, condition);
        MoveTo(condition).Do('[');
        return this;
    }
    
    public ProgramBuilder EndIf() {
        var (type, condition, locals) = ControlBlocks.Current;
        if (type != BlockType.If) throw new ($"Tried to end if, but was actually ${type}");
		foreach (int var in locals.ToList()) Release(var);
        MoveTo(condition).Zero(condition).Do(']');
        ControlBlocks.Pop();
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
    public ProgramBuilder Zero(int var) {
        using (Loop(var)) Decrement(var);
        return this;
    }
    
    public ProgramBuilder Release(int var) {
        if (!Allocated[var]) throw new ("Can't release what was not yours");
        if (Named[var]) Comment($"[{ var }]: ");
        Named[var] = Allocated[var] = false;
		ControlBlocks.Current.LocalVariables.Remove(var);
        return this;
    }
    
    private bool[] Named = new bool[MemorySize];
    public ProgramBuilder Allocate(out int var, byte value, string? debugName = default) {
        var = Array.IndexOf(Allocated, false);
        if (var >= Range - 1) throw new ("failed to allocate. no memory free.");
        if (debugName is string) {
            Comment($"[{ var }]: { debugName }");
            Named[var] = true;
        }
        Allocated[var] = true;
		ControlBlocks.Current.LocalVariables.Add(var);
        Zero(var).Increment(var, value);
        return this;
    }
    
    public void Release(params int[] vars) {
        foreach (int var in vars) Release(var);
    }
    
    private int? Range = null;
    public ProgramBuilder AllocateRange(out int range, int additionalAllocationBuffer = 0) {
        if (Range.HasValue) throw new ("can't allocate multiple ranges");
        range = Array.LastIndexOf(Allocated, true) + 2 + additionalAllocationBuffer;
        Comment($"Allocated range at { range } leaving buffer of { additionalAllocationBuffer }");
        Range = range;
        return this;
    }
    
    public void ReleaseRange() {
        if (!Range.HasValue) throw new ("no range to release");
        Range = null;
    }
    
    public ProgramBuilder Increment(int x, int times = 1) {
        times &= 0xff;
        if (times == 0) return this;
        MoveTo(x);
        if (times <= 128) Do(new string('+', times));
        else Do(new string('-', 256 - times));
        return this;
    }

    public ProgramBuilder Decrement(int x, int times = 1) => Increment(x, -times);
        
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
        Zero(target1).Zero(target2);
        using (Loop(source)) {
            Decrement(source);
            Increment(target1);
            Increment(target2);
        }
        return this;
    }
        
    /// <summary>x=y;</summary>
    public ProgramBuilder Copy(int target, int source) {
        Allocate(out int temp, 0);
        MoveTwice(target, temp, source);
        AddAndZero(source, temp);
        Release(temp);
        return this;
    }
    
    /// <summary>declare variable copying value</summary>
    public ProgramBuilder AllocateAndCopy(out int target, int var, string? debugName = default) {
        Allocate(out target, 0, debugName);
        Copy(target, var);
        return this;
    }
    
    /// <summary>acc+=operand;</summary>
    public ProgramBuilder Add(int acc, int operand) {
        AllocateAndCopy(out int temp, operand);
        AddAndZero(acc, temp);
        Release(temp);
        return this;
    }
    
    /// <summary>target = operand1 * operand2; operand2 = 0;</summary>
    public ProgramBuilder Mul(int target, int operand1, int operand2) {
        Zero(target);
        using (Loop(operand2)) {
            Add(target, operand1);
            Decrement(operand2);
        }
        return this;
    }
    
    /// <summary>target = operand == 0 ? 1 : 0; operand = 0;</summary>
    public ProgramBuilder Not(int target, int operand) {
        Zero(target).Increment(target);
        using(If(operand)) {
            Zero(target);
        }
        return this;
    }

	/// <summary>if (condition == 0) { ... } condition = 0; </summary>
	public ProgramBuilder IfNot(int condition) {
		Allocate(out int not, 0);
		Not(not, condition);
		If(not);
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
        AllocateAndCopy(out int progress, denominator, nameof(progress));
        Zero(target);
        using (Loop(numerator)) {
            Decrement(numerator);
            Decrement(progress);
            Allocate(out int ztest, 0, nameof(ztest));
            AllocateAndCopy(out int progresscopy, progress, nameof(progresscopy));
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
        Zero(target);
        using (Loop(numerator)) {
            Decrement(numerator).Increment(target);
            
            AllocateAndCopy(out int targetCopy, target, nameof(targetCopy));
            AllocateAndCopy(out int divisorCopy, divisor, nameof(divisorCopy));
            Eq(targetCopy, divisorCopy);
            using (If(targetCopy)) {
                Zero(target);
            }
            Release(targetCopy, divisorCopy);
        }
        return this;
    }
    
    /// <summary>div = numerator / divisor; mod = numerator % divisor; numerator = 0;</summary>
    public ProgramBuilder DivMod(int div, int mod, int numerator, int divisor) {
        Zero(div).Zero(mod);
        using (Loop(numerator)) {
            Decrement(numerator).Increment(mod);
            
            AllocateAndCopy(out int modCopy, mod, nameof(modCopy));
            AllocateAndCopy(out int divisorCopy, divisor, nameof(divisorCopy));
            Eq(modCopy, divisorCopy);
            using (If(modCopy)) {
                Zero(mod);
                Increment(div);
            }
            Release(modCopy, divisorCopy);
        }
        return this;
    }
    
    public ProgramBuilder WriteChar(char c) {
        Allocate(out int nl, 0);
        Increment(nl, (int)c);
        Do('.');
        Release(nl);
        return this;
    }
    
    public ProgramBuilder NewLine() => WriteChar('\n');
    
    public ProgramBuilder WriteString(string s) {
        Comment("Writing " + s);
        Allocate(out int ch, 0);
		int last = 0;
        foreach (char c in s) {
            Increment(ch, c - last);
            Do('.');
            last = c;
        }
        Release(ch);
        return this;
    }
    
    public ProgramBuilder WriteDigit(int var) {
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
	
	public ProgramBuilder MoveToStack(int var) {
		Comment("Moving to stack");
		MoveTo(0).Do("<<[<]>").Do("[[-<+>]>]>");
		AddAndZero(-2, var);
		return this;
	}
	
	/// <summary>acc += pop();</summary>
	public ProgramBuilder PopStackAdd(int acc) {
		Comment("Pop stack add");
		AddAndZero(acc, -2);
		Do("<[[->+<]<]").Do(">>[>]"); Head = -1;
		return this;
	}
    
	/// <summary>print(n); n = 0;</summary>
    public ProgramBuilder WriteNumber(int n) {
        Allocate(out int ten, 10, nameof(ten));
        Allocate(out int _n, 0, nameof(_n));
        using (Loop(n)) {
			Allocate(out int digit, 0, nameof(digit));
            DivMod(_n, digit, n, ten);
            Increment(digit, 48);
            MoveToStack(digit);
			Release(digit);
            AddAndZero(n, _n);
        }
        Release(_n, ten);
        MoveTo(-2).Do("[.<]>").Do("[[-]>]");
		Head = - 1;
        return this;
    }

	public ProgramBuilder AllocateAndReadNumber(out int num) {
    	Allocate(out num, 0, nameof(num));
	    Allocate(out int digit, 0, nameof(digit));    
	    Allocate(out int ten, 10, nameof(ten));
	    MoveTo(digit).Do(',').Decrement(digit, 10);
	    using (Loop(digit)) {
	        Allocate(out int newn, 0, nameof(newn));
	        Mul(newn, ten, num);
	        AddAndZero(num, newn);
	        Release(newn);
	        
	        Decrement(digit, 38).AddAndZero(num, digit);
	        Zero(digit).Increment(digit, 10).Do(',').Decrement(digit, 10);
	    }
	    Release(ten, digit);
		return this;
	}
	
	public ProgramBuilder Square(int target, int num) {
		AllocateAndCopy(out int _num, num);
		Mul(target, num, _num);
		Release(_num);
		return this;
	}
}
