using System;
using System.Collections.Generic;

namespace MentalCaressCompiler {
    static class CodeGen {
		public static string FromAST(IEnumerable<AST.Statement> statements, bool comments = false) {
			ProgramBuilder builder = new() { GenerateComments = comments };
			Dictionary<string, int> vars = new();
	
			void Build(AST.Statement statement) {
				switch (statement) {
					case AST.Declaration { Value: AST.NumberLiteral val } decl: {
						builder.Allocate(out int var, val.Value);
						if (vars.ContainsKey(decl.Id.Name)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id.Name] = var;
						break;
					}
					case AST.Declaration { Value: AST.Identifier source } decl: {
						builder.AllocateAndCopy(out int var, vars[source.Name]);
						if (vars.ContainsKey(decl.Id.Name)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id.Name] = var;
						break;
					}
					case AST.Copy { Source: AST.NumberLiteral num } copy:
						builder.Zero(vars[copy.Target.Name]);
						builder.Increment(vars[copy.Target.Name], num.Value);
						break;
			
					case AST.Block { Type: AST.BlockType.Loop, Control: var control, Body: var body }:
						builder.Loop(vars[control.Name]);
						foreach (var s in body) Build(s);
						builder.EndLoop();
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
				
					case AST.Action0 { Type: "writeline" }:
						builder.NewLine();
						break;

					case AST.Action1 { Type: "write" } op:
						builder.MoveTo(vars[op.Id.Name]).Do('.');
						break;
					case AST.Action1 { Type: "readnum" } op:
						builder.ReadNumber(vars[op.Id.Name]);
						break;

					case AST.WriteText wl:
						builder.WriteString(wl.Message);
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
