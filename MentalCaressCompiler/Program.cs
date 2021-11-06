using System;
using System.IO;
using System.Linq;

namespace MentalCaressCompiler {
    class Program {
        static void Main(string[] args) {
            foreach (var arg in args) Console.WriteLine(arg);
            string inFile = args.Single(a => !a.StartsWith('-'));
            string? outFile = args.SingleOrDefault(a => a.StartsWith("-out="))?[5..];
            string source = File.ReadAllText(inFile);

            try {
                string bf = Compile(source);
                if (string.IsNullOrEmpty(outFile)) {
                    Console.WriteLine(bf);
                }
                else {
                    File.WriteAllText(outFile, bf);
                }
            }
            catch (Sprache.ParseException ex) {
				Console.Error.WriteLine("{0}:{1} {2}", ex.Position.Line, ex.Position.Column, ex.Message);
            }
        }

		static string Compile(string mcsource) {
            var ast = MentalCaressParsers.ParseProgram(mcsource);
            var bf = CodeGen.FromAST(ast);
            return bf;
        }
    }
}
