<Query Kind="Program">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
</Query>

void Main() {
    ProgramBuilder builder = new() { GenerateComments = true };
    
    //*
    int n = builder.Allocate(nameof(n));
    int digit = builder.Allocate(nameof(digit));    
    int ten = builder.Allocate(nameof(ten));
    builder.Increment(ten, 10);
    
    builder.MoveTo(digit).Do(',').Decrement(digit, 10);
    using (builder.Loop(digit)) {
        int newn = builder.Allocate(nameof(newn));
        builder.Mul(newn, ten, n);
        builder.AddAndZero(n, newn);
        builder.Release(newn);
        
        builder.Decrement(digit, 38).AddAndZero(n, digit);
        builder.MoveTo(digit).Do(',').Decrement(digit, 10);
    }
    builder.Release(ten, digit);
    
    int factor = builder.Allocate(nameof(factor));
    builder.Increment(factor, 1);
    
    builder.Decrement(n);
    using (builder.Loop(n)) {
        builder.Increment(n);
        builder.Increment(factor);
        
        int ncopy = builder.AllocateAndCopy(n, nameof(ncopy));
        int div = builder.Allocate(nameof(div));
        int mod = builder.Allocate(nameof(mod));
        builder.DivMod(div, mod, ncopy, factor);
        builder.Release(ncopy);
        
        int divides = builder.Allocate(nameof(divides));
        builder.Not(divides, mod);
        using(builder.If(divides)) {
            builder.Comment("Found divisor");
            int fcopy = builder.AllocateAndCopy(factor, nameof(fcopy));
            builder.WriteNumber(fcopy).Release(fcopy).NewLine();
            builder.Zero(n).AddAndZero(n, div);
            builder.Comment("Compensating factor");
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

class ProgramBuilder : IDisposable {
    const int MemorySize = 256;

    private enum BlockType { Top, Loop, If }
    private record Block(BlockType Type, int ControlVariable);

    private class BlockStack {
        private readonly Stack<Block> ControlBlocks = new();
        
        public BlockStack() {
            ControlBlocks.Push(new(BlockType.Top, -1));
        }

        public void Push(BlockType type, int controlVariable) {
            ControlBlocks.Push(new(type, controlVariable));
        }
        
        public Block Pop() => ControlBlocks.Pop();
        
        public Block Current => ControlBlocks.Peek();
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
        var (type, condition) = ControlBlocks.Current;
        if (type != BlockType.Loop) throw new ($"Tried to end loop, but was actually ${type}");
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
        var (type, condition) = ControlBlocks.Current;
        if (type != BlockType.If) throw new ($"Tried to end if, but was actually ${type}");
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
    
    private bool[] Named = new bool[MemorySize];
    public int Allocate(string? debugName = default) => AllocateConstant(0, debugName);
    
    public ProgramBuilder Release(int var) {
        if (!Allocated[var]) throw new ("Can't release what was not yours");
        if (Named[var]) Comment($"[{ var }]: ");
        Named[var] = Allocated[var] = false;
        return this;
    }
    
    public void Release(params int[] vars) {
        foreach (int var in vars) Release(var);
    }
    
    public int AllocateConstant(byte value, string? debugName = default) {
        int var = Array.IndexOf(Allocated, false);
        if (var >= Range - 1) throw new ("failed to allocate. no memory free.");
        if (debugName is string) {
            Comment($"[{ var }]: { debugName }");
            Named[var] = true;
        }
        Allocated[var] = true;
        Zero(var).Increment(var, value);
        return var;
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
    
    /// <summary>x = x == y; y = ???;</summary>
    public ProgramBuilder Eq(int x, int y) {
        SubAndZero(y, x);
        Not(x, y);
        return this;
    }
    
    /// <summary>target = numerator/denominator; numerator = 0;</summary>
    public ProgramBuilder Div(int target, int numerator, int denominator) {
        int progress = AllocateAndCopy(denominator, nameof(progress));
        Zero(target);
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
        Zero(target);
        using (Loop(numerator)) {
            Decrement(numerator).Increment(target);
            
            int targetCopy = AllocateAndCopy(target, nameof(targetCopy));
            int divisorCopy = AllocateAndCopy(divisor, nameof(divisorCopy));
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
        Comment("starting divmod loop");
        using (Loop(numerator)) {
            Comment("decrement numerator, increment mod");
            Decrement(numerator).Increment(mod);
            
            int modCopy = AllocateAndCopy(mod, nameof(modCopy));
            int divisorCopy = AllocateAndCopy(divisor, nameof(divisorCopy));
            Comment("eq modcopy divisorcopy");
            Eq(modCopy, divisorCopy);
            Comment("if modcopy");
            using (If(modCopy)) {
                Comment("zeroing mod");
                Zero(mod);
                Comment("increment div");
                Increment(div);
                Comment("ending if block");
            }
            Release(modCopy, divisorCopy);
            Comment("ending divmod loop");
        }
        return this;
    }
    
    public ProgramBuilder WriteChar(char c) {
        int nl = Allocate();
        Increment(nl, (int)c);
        Do('.');
        Release(nl);
        return this;
    }
    
    public ProgramBuilder NewLine() => WriteChar('\n');
    
    public ProgramBuilder WriteString(string s) {
        Comment("Writing " + s);
        int ch = Allocate(), last = 0;
        foreach (char c in s) {
            Increment(ch, c - last);
            Do('.');
            last = c;
        }
        Release(ch);
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
    
    public ProgramBuilder WriteNumber(int n) {
        int ten = Allocate(nameof(ten));
        Increment(ten, 10);
        int ncopy = Allocate(nameof(ncopy));
        int range = AllocateRange(additionalAllocationBuffer: 3);
        using (Loop(n)) {
            Comment("Pushing range");
            PushRange();
            Comment("Doing divmod");
            DivMod(ncopy, range, n, ten);
            Comment("Increment 48");
            Increment(range, 48);
            Comment("Move back to n");
            AddAndZero(n, ncopy);
            Comment("Finishing loop");
        }
        Release(ncopy, ten);
        Comment("Moving to range");
        MoveTo(range);
        Comment("Outputting");
        Do("[.>]").Do("<[[-]<]"); Head = range - 1;
        ReleaseRange();
        return this;
    }
}
