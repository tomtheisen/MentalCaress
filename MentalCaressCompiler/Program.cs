using System;
using System.IO;
using System.Linq;

namespace MentalCaressCompiler {
    class Program {
        static void Main(string[] args) {
            string inFile = args.Single(a => !a.StartsWith('-'));
            string? outFile = args.SingleOrDefault(a => a.StartsWith("-out="))?[5..];
            var comments = 
                args.Contains("-c3") ? CommentVerbosity.SourceAndCodeGen
                : args.Contains("-c2") ? CommentVerbosity.SourceAndAllocationNames
                : args.Contains("-c1") ? CommentVerbosity.Source
                : CommentVerbosity.None;
            string source = File.ReadAllText(inFile);

            try {
                string bf = Compile(source, comments);
                Console.WriteLine("Size: {0}", bf.Length);
                if (string.IsNullOrEmpty(outFile)) {
                    Console.WriteLine(bf);
                }
                else {
                    File.WriteAllText(outFile, bf);
                }
            }
            catch (Sprache.ParseException ex) {
				Console.Error.WriteLine(ex.Message);
            }
        }

		static string Compile(string mcsource, CommentVerbosity comments) {
            var ast = MentalCaressParsers.ParseProgram(mcsource);
            var bf = CodeGen.FromAST(ast, comments);
            return bf;
        }
    }
}
