using System;
using System.Collections.Generic;

namespace MentalCaressCompiler {
    public enum CommentVerbosity {
		None,
		Source,
		SourceAndAllocationNames,
		SourceAndCodeGen,
    }
    public static class CodeGen {
        record VariableStack(Dictionary<AST.Identifier, int> Current, VariableStack? Parents) {
            public void Release(AST.Identifier var) {
                if (Current.Remove(var)) return;
                if (Parents is null) throw new Exception($"Unable to release { var } that was never had");
                Parents.Release(var);
            }

            public bool Contains(AST.Identifier id) => Current.ContainsKey(id)
                || Parents is VariableStack p && p.Contains(id);

            public int this[AST.Identifier id] => Current.TryGetValue(id, out int pos)
                ? pos : Parents?[id] ?? throw new KeyNotFoundException($"{id} not found");

            public void Add(AST.Identifier declaration, int loc) {
                if (Contains(declaration)) throw new($"Duplicate declaration { declaration.Name }");
                Current.Add(declaration, loc);
            }
        }

		public static string FromAST(IEnumerable<AST.Statement> statements, CommentVerbosity comments = 0) {
			ProgramBuilder builder = new();
	
			void Build(IEnumerable<AST.Statement> statements, VariableStack? parentVars) {
                VariableStack vars = new (new(), parentVars);

				foreach (var statement in statements) {
					if (statement.SourceText is string sourceText && comments >= CommentVerbosity.Source) {
						builder.GenerateComments = true;
						builder.Comment(sourceText);
					}
					builder.GenerateComments = comments >= CommentVerbosity.SourceAndCodeGen;
                    try {
                        switch (statement) {
                            case AST.Comment comment:
                                // already included from the source line
                                break;

                            case AST.Declaration { Value: AST.NumberLiteral val } decl: {
                                builder.GenerateComments |= comments >= CommentVerbosity.SourceAndAllocationNames;
                                builder.Allocate(out int var, val.Value, decl.Id.Name);
                                vars.Add(decl.Id, var);
                                break;
                            }
                            case AST.Declaration { Value: AST.Identifier source } decl: {
                                builder.AllocateAndCopy(out int var, vars[source], decl.Id.Name);
                                vars.Add(decl.Id, var);
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
                            case AST.OperateAssign { Operator: '-', B: AST.Identifier b } assign
                                when assign.Target == assign.A: {
                                builder.AllocateAndCopy(out int _b, vars[b], nameof(_b));
                                builder.SubAndZero(vars[assign.Target], _b);
                                builder.Release(_b);
                                break;
                            }
                            case AST.OperateAssign { A: AST.Identifier a, Operator: '-', B: AST.Identifier b } assign: {
                                builder.Copy(vars[assign.Target], vars[a]);
                                builder.AllocateAndCopy(out int _b, vars[b], nameof(_b));
                                builder.SubAndZero(vars[assign.Target], _b);
                                builder.Release(_b);
                                break;
                            }
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
                            case AST.OperateAssign { Operator: '%', B: AST.Identifier b } mod
                                when mod.Target == mod.A: {
                                builder.AllocateAndCopy(out int _a, vars[mod.Target], nameof(_a));
                                builder.Mod(vars[mod.Target], _a, vars[b]);
                                builder.Release(_a);
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
                            case AST.Action1 { Type: "writenum" } op: {
                                builder.AllocateAndCopy(out int towrite, vars[op.Id], nameof(towrite));
                                builder.WriteNumber(towrite);
                                builder.Release(towrite);
                                break;
                            }
                            case AST.Action1 { Type: "readnum" } op:
                                builder.ReadNumber(vars[op.Id]);
                                break;
                            case AST.Action1 { Type: "release" } op:
                                builder.Release(vars[op.Id]);
                                vars.Release(op.Id);
                                break;

                            case AST.Block { Type: AST.BlockType.Loop } loop:
                                builder.Loop(vars[loop.Control]);
                                Build(loop.Body, vars);
                                builder.EndLoop();
                                break;
                            case AST.Block { Type: AST.BlockType.If } @if:
                                builder.IfAndZero(vars[@if.Control]);
                                Build(@if.Body, vars);
                                builder.EndIf();
                                break;
                            case AST.Block { Type: AST.BlockType.IfRelease } @if:
                                builder.IfRelease(vars[@if.Control]);
                                vars.Release(@if.Control);
                                Build(@if.Body, vars);
                                builder.EndIf();
                                break;
                            case AST.Block { Type: AST.BlockType.IfNot } ifnot: {
                                builder.AllocateAndCopy(out int _control, vars[ifnot.Control], nameof(_control));
                                builder.IfNotRelease(_control);
                                Build(ifnot.Body, vars);
                                builder.EndIf();
                                break;
                            }
                            case AST.Block { Type: AST.BlockType.IfNotRelease } ifnot:
                                builder.IfNotRelease(vars[ifnot.Control]);
                                vars.Release(ifnot.Control);
                                Build(ifnot.Body, vars);
                                builder.EndIf();
                                break;
                            case AST.Repeat repeat:
                                builder.Allocate(out int times, repeat.Times, nameof(times));
                                builder.Loop(times).Decrement(times);
                                Build(repeat.Body, vars);
                                builder.EndLoop().Release(times);
                                break;

                            default: throw new($"No codegen for ast node ${ statement }");
                        }
                    }
                    catch (Exception ex) {
                        throw new Exception($"Codegen: {statement.SourceText}", ex);
                    }
				}
			}
	
			Build(statements, default);
	
			return builder.Build();
		}
    }
}
