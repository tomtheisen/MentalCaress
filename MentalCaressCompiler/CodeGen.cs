﻿using System;
using System.Collections.Generic;

namespace MentalCaressCompiler {
    public static class CodeGen {
		public static string FromAST(IEnumerable<AST.Statement> statements, bool comments = false) {
			ProgramBuilder builder = new() { GenerateComments = comments };
			Dictionary<AST.Identifier, int> vars = new();
	
			void Build(AST.Statement statement) {
				switch (statement) {
					case AST.Comment comment:
						builder.Comment(comment.Message);
						break;

					case AST.Declaration { Value: AST.NumberLiteral val } decl: {
						builder.Allocate(out int var, val.Value, decl.Id.Name);
						if (vars.ContainsKey(decl.Id)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id] = var;
						break;
					}
					case AST.Declaration { Value: AST.Identifier source } decl: {
						builder.AllocateAndCopy(out int var, vars[source], decl.Id.Name);
						if (vars.ContainsKey(decl.Id)) throw new ($"Duplicate declaration { decl.Id.Name }");
						vars[decl.Id] = var;
						break;
					}
					case AST.Copy { Source: AST.NumberLiteral num } copy:
						builder.Zero(vars[copy.Target]);
						builder.Increment(vars[copy.Target], num.Value);
						break;
					case AST.Copy { Source: AST.Identifier source } copy:
						builder.Copy(vars[copy.Target], vars[source]);
						break;

					case AST.NotAssign not: {
						builder.AllocateAndCopy(out int _notop, vars[not.Value], nameof(_notop));
						builder.Not(vars[not.Target], _notop);
						builder.Release(_notop);
						break;
                    }
					case AST.OperateAssign { Operator: '+', B: AST.NumberLiteral b } assign 
						when assign.Target == assign.A:
						builder.Increment(vars[assign.Target], b.Value);
						break;
					case AST.OperateAssign { A: AST.NumberLiteral a, Operator: '+' } assign 
						when assign.Target == assign.B:
						builder.Increment(vars[assign.Target], a.Value);
						break;
					case AST.OperateAssign { Operator: '+', B: AST.Identifier b } assign 
						when assign.Target == assign.A: {
						builder.AllocateAndCopy(out int _b, vars[b], nameof(_b));
						builder.AddAndZero(vars[assign.Target], _b);
						builder.Release(_b);
						break;
					}
					case AST.OperateAssign { A: AST.Identifier a, Operator: '+' } assign 
						when assign.Target == assign.B: {
						builder.AllocateAndCopy(out int _a, vars[a], nameof(_a));
						builder.AddAndZero(vars[assign.Target], _a);
						builder.Release(_a);
						break;
					}
					case AST.OperateAssign { A: AST.Identifier a, Operator: '+', B: AST.Identifier b } assign: {
						builder.AllocateAndCopy(out int _b, vars[b], nameof(_b));
						builder.Copy(vars[assign.Target], vars[a]);
						builder.AddAndZero(vars[assign.Target], _b);
						builder.Release(_b);
						break;
					}
					case AST.OperateAssign { Operator: '-', B: AST.NumberLiteral b } assign 
						when assign.Target == assign.A:
						builder.Decrement(vars[assign.Target], b.Value);
						break;
					case AST.OperateAssign { A: AST.Identifier a, Operator: '-', B: AST.NumberLiteral b } assign:
						builder.Copy(vars[assign.Target], vars[a]);
						builder.Decrement(vars[assign.Target], b.Value);
						break;
                    case AST.OperateAssign { A: AST.Identifier a, Operator: '*', B: AST.NumberLiteral b } assign
                        when assign.Target != a: {
						builder.Allocate(out int _b, b.Value, nameof(_b));
						builder.Mul(vars[assign.Target], vars[a], _b);
						builder.Release(_b);
						break;
					}
                    case AST.OperateAssign { A: AST.Identifier a, Operator: '/', B: AST.Identifier b } div
						when div.Target != div.A && div.Target != div.B: { 
						builder.AllocateAndCopy(out int _numerator, vars[a], nameof(_numerator));
						builder.Div(vars[div.Target], _numerator, vars[b]);
						builder.Release(_numerator);
						break;
					}
					case AST.OperateAssign { A: AST.Identifier a, Operator: '/', B: AST.NumberLiteral b } div
						when div.Target != div.A: {
						builder.AllocateAndCopy(out int _numerator, vars[a], nameof(_numerator));
						builder.Allocate(out int _denominator, b.Value, nameof(_denominator));
						builder.Div(vars[div.Target], _numerator, _denominator);
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
						builder.MoveTo(vars[op.Id]).Do('.');
						break;
					case AST.Action1 { Type: "readnum" } op:
						builder.ReadNumber(vars[op.Id]);
						break;

					case AST.Block { Type: AST.BlockType.Loop } loop:
						builder.Loop(vars[loop.Control]);
						foreach (var s in loop.Body) Build(s);
						builder.EndLoop();
						break;
					case AST.Block { Type: AST.BlockType.If } @if:
						builder.If(vars[@if.Control]);
						foreach (var s in @if.Body) Build(s);
						builder.EndIf();
						break;
					case AST.Block { Type: AST.BlockType.IfNot } ifnot:
						builder.IfNot(vars[ifnot.Control]);
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
