﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace MentalCaressCompiler {
	public static class MentalCaressParsers {
		static Parser<T> AnyOf<T>(params Parser<T>[] parsers) => parsers.Aggregate(Parse.Or);
	
		static Parser<string> AnyOf(params string[] s) 
			=> s.Select(e => Parse.String(e).Text()).Aggregate(Parse.Or);

		static Parser<string> Indent => Parse.Chars(' ', '\t').Many().Text();
	
		static Parser<string> Terminator => 
			from w in Parse.Chars(' ', '\t').Many()
			from nl in Parse.LineTerminator
			select "";

		static Parser<IOption<string>> LinePreview =>
			Parse.AnyChar.Until(Parse.LineTerminator).Text().Preview();
		
		static Parser<AST.Identifier> Identifier => Parse.LetterOrDigit.Or(Parse.Char('_'))
			.AtLeastOnce()
			.Contained(Parse.Char(' ').Many(), Parse.Char(' ').Many())
			.Text()
			.Select(lod => new AST.Identifier(lod));
		
		static Parser<AST.NumberLiteral> NumberLiteral => 
			from s1 in Parse.Char(' ').Many()
			from minus in Parse.Char('-').Optional()
			from digits in Parse.Number
			from s2 in Parse.Char(' ').Many()
			let val = int.Parse(digits)
			where val <= byte.MaxValue
			select new AST.NumberLiteral((byte)(minus.IsDefined ? -val : val));
		
		static Parser<AST.NumberLiteral> CharLiteral =>
			from s1 in Parse.Char(' ').Many()
			from ch in Parse.AnyChar.Contained(Parse.Char('\''), Parse.Char('\''))
			from s2 in Parse.Char(' ').Many()
			select new AST.NumberLiteral((byte)ch);
		
		static Parser<AST.NumberLiteral> Literal => NumberLiteral.Or(CharLiteral);
		
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

		public static Parser<AST.OperateAssign> AugmentedAssign =>
			from target in Identifier
			from op in Parse.Chars("-+/*%")
			from eq in Parse.Char('=')
			from a in Value
			select new AST.OperateAssign(target, target, op, a);

		public static Parser<AST.NotAssign> NotAssign =>
			from target in Identifier
			from eq in Parse.Char('=')
			from s1 in Parse.Char(' ').Many()
			from not in Parse.String("not")
			from s2 in Parse.Char(' ').AtLeastOnce()
			from val in Identifier
			select new AST.NotAssign(target, val);
		
		public static Parser<AST.Copy> Copy =>
			from target in Identifier
			from eq in Parse.Char('=')
			from source in Value
			select new AST.Copy(target, source);
		
		public static Parser<AST.Action0> Action0 =>
			from action in AnyOf("writeline")
			select new AST.Action0(action);

		public static Parser<AST.Action1> Action1 =>
			from action in AnyOf("readnum", "read", "writenum", "write", "release")
			from s1 in Parse.Char(' ').AtLeastOnce()
			from id in Identifier
			select new AST.Action1(action, id);

		public static Parser<AST.WriteText> WriteText =>
			from action in Parse.String("writetext")
			from s1 in Parse.Char(' ').AtLeastOnce()
			from q1 in Parse.Char('"')
			from message in Parse.AnyChar.Until(Parse.Char('"')).Text()
			select new AST.WriteText(message);
	
		public static Parser<AST.BlockType> BlockType 
			=> AnyOf(
					Parse.String("ifnot release").Select(_ => AST.BlockType.IfNotRelease),
					Parse.String("if release").Select(_ => AST.BlockType.IfRelease),
					Parse.String("ifnot").Select(_ => AST.BlockType.IfNot),
					Parse.String("if").Select(_ => AST.BlockType.If),
					Parse.String("loop").Select(_ => AST.BlockType.Loop))
				.ThenNot(Parse.LetterOrDigit);
		
		public static Parser<AST.Block> Block => 
			from type in BlockType
			from control in Identifier
			from open in Parse.Char('{')
			from t1 in Terminator
			from body in StatementList
			from indent in Indent
			from close in Parse.Char('}')
			select new AST.Block(type, control, body.ToArray());

		public static Parser<AST.Repeat> Repeat =>
			from rp in Parse.String("repeat")
			from times in NumberLiteral
			from open in Parse.Char('{')
			from t1 in Terminator
			from body in StatementList
			from indent in Indent
			from close in Parse.Char('}')
			select new AST.Repeat(times.Value, body.ToArray());
	
		public static Parser<AST.Comment> Comment =>
			from s in Parse.Chars(" \t").Many()
			from hash in Parse.Char('#')
			from content in Parse.CharExcept("\r\n").Many().Text()
			select new AST.Comment(content);
	
		public static Parser<AST.Statement> Statement => 
			from line in LinePreview
			from s1 in Indent
			from statement in AnyOf<AST.Statement>(
				Declaration, 
				NotAssign,
				OperateAssign,
				AugmentedAssign,
				Copy, 
				Action0, 
				Action1, 
				WriteText,
				Block,
				Repeat,
				Comment)
			from s3 in Terminator
			select statement with { SourceText = line.GetOrDefault() };
		
		public static Parser<string> BlankLines =>
			Parse.Chars(" \t").Many().Then(_ => Parse.LineTerminator).Many().Select(_ => "");
		
		public static Parser<IEnumerable<AST.Statement>> StatementList =>
			Statement.Contained(BlankLines, BlankLines).Many();
		
		public static Parser<IEnumerable<AST.Statement>> Program =>
			StatementList.End();

		public static IEnumerable<AST.Statement> ParseProgram(string source) 
			=> Program.Parse(source);
	}
}
