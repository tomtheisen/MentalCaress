﻿namespace MentalCaressCompiler.AST {
	public record Value();
	public record Identifier(string Name) : Value;
	public record NumberLiteral(byte Value) : Value;

	public record Statement() {
        public string? SourceText { get; init; }
        public Sprache.Position? Position { get; init; }
    }
	public record Declaration(Identifier Id, Value Value) : Statement;
	public record OperateAssign(Identifier Target, Value A, char Operator, Value B) : Statement;
    public record DivModAssign(Identifier Div, Identifier Mod, Value A, Value B) : Statement;
	public record NotAssign(Identifier Target, Identifier Value) : Statement;
	public record Copy(Identifier Target, Value Source) : Statement;
	public record Comment(string Message) : Statement;
	
	public record Action0(string Type) : Statement;
	public record Action1(string Type, Identifier Id) : Statement;
	public record WriteText(string Message) : Statement;
	
	public enum BlockType { Loop, If, IfNot, IfRelease, IfNotRelease }
	public record Block(BlockType Type, Identifier Control, Statement[] Body) : Statement;
	public record Repeat(byte Times, Statement[] Body) : Statement;
}
