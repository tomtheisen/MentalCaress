namespace MentalCaressCompiler.AST {
	public record Value();
	public record Identifier(string Name) : Value;
	public record NumberLiteral(byte Value) : Value;

	public record Statement();
	public record Declaration(Identifier Id, Value Value) : Statement;
	public record OperateAssign(Identifier Target, Value A, char Operator, Value B) : Statement;
	public record Copy(Identifier Target, Value Source) : Statement;
	
	public record Action(string Type, Identifier Id) : Statement;
	
	public enum BlockType { Loop, If, IfNot }
	public record Block(BlockType Type, Identifier Control, Statement[] Body) : Statement;
}
