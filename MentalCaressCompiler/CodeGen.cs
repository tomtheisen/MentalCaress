using System;
using System.Collections.Generic;

namespace MentalCaressCompiler {
    public static class CodeGen {
		public static string FromAST(IEnumerable<AST.Statement> statements, bool comments = false) {
			ProgramBuilder builder = new() { GenerateComments = comments };
			Dictionary<string, int> vars = new();
	
			void Build(AST.Statement statement) {
				switch (statement) {
					case AST.Comment comment:
						builder.Comment(comment.Message);
						break;

					case AST.Declaration { Value: AST.NumberLiteral val } decl: {
						builder.Allocate(out int var, val.Value, decl.Id.Name);
						if (vars.ContainsKey(decl.Id.Name)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id.Name] = var;
						break;
					}
					case AST.Declaration { Value: AST.Identifier source } decl: {
						builder.AllocateAndCopy(out int var, vars[source.Name], decl.Id.Name);
						if (vars.ContainsKey(decl.Id.Name)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id.Name] = var;
						break;
					}
					case AST.Copy { Source: AST.NumberLiteral num } copy:
						builder.Zero(vars[copy.Target.Name]);
						builder.Increment(vars[copy.Target.Name], num.Value);
						break;
			
					case AST.OperateAssign { Operator: '+', B: AST.NumberLiteral b } assign 
						when assign.Target == assign.A:
						builder.Increment(vars[assign.Target.Name], b.Value);
						break;
					case AST.OperateAssign { A: AST.NumberLiteral a, Operator: '+' } assign 
						when assign.Target == assign.B:
						builder.Increment(vars[assign.Target.Name], a.Value);
						break;
					case AST.OperateAssign { Operator: '-', B: AST.NumberLiteral b } assign 
						when assign.Target == assign.A:
						builder.Decrement(vars[assign.Target.Name], b.Value);
						break;
					case AST.OperateAssign { A: AST.Identifier a, Operator: '-', B: AST.NumberLiteral b } assign:
						builder.Copy(vars[assign.Target.Name], vars[a.Name]);
						builder.Decrement(vars[assign.Target.Name], b.Value);
						break;
					case AST.OperateAssign { A: AST.Identifier a, Operator: '/', B: AST.Identifier b } div
						when div.Target != div.A && div.Target != div.B: { 
						builder.AllocateAndCopy(out int _numerator, vars[a.Name], nameof(_numerator));
						builder.Div(vars[div.Target.Name], _numerator, vars[b.Name]);
						builder.Release(_numerator);
						break;
					}
					case AST.OperateAssign { A: AST.Identifier a, Operator: '/', B: AST.NumberLiteral b } div
						when div.Target != div.A: {
						builder.AllocateAndCopy(out int _numerator, vars[a.Name], nameof(_numerator));
						builder.Allocate(out int _denominator, b.Value, nameof(_denominator));
						builder.Div(vars[div.Target.Name], _numerator, _denominator);
						builder.Release(_numerator, _denominator);
						break;
					}
					case AST.Action0 { Type: "writeline" }:
						builder.NewLine();
						break;
					case AST.WriteText wl:
						builder.WriteString(wl.Message);
						break;
				
					case AST.Action1 { Type: "write" } op:
						builder.MoveTo(vars[op.Id.Name]).Do('.');
						break;
					case AST.Action1 { Type: "readnum" } op:
						builder.ReadNumber(vars[op.Id.Name]);
						break;

					case AST.Block { Type: AST.BlockType.Loop } loop:
						builder.Loop(vars[loop.Control.Name]);
						foreach (var s in loop.Body) Build(s);
						builder.EndLoop();
						break;
					case AST.Block { Type: AST.BlockType.If } @if:
						builder.If(vars[@if.Control.Name]);
						foreach (var s in @if.Body) Build(s);
						builder.EndIf();
						break;
					case AST.Block { Type: AST.BlockType.IfNot } ifnot:
						builder.IfNot(vars[ifnot.Control.Name]);
						foreach (var s in ifnot.Body) Build(s);
						builder.EndIf();
						break;

					default: throw new ($"No codegen for ast node ${ statement }");
				}
			}
	
			foreach (var statement in statements) {
				Build(statement);
			}
	
			return builder.Build();
		}
    }
}
