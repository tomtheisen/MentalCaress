using System;
using System.IO;
using System.Linq;

namespace MentalCaressCompiler {
    class Program {
        static void Main(string[] args) {
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
				Console.Error.WriteLine(ex.Message);
            }
        }

		static string Compile(string mcsource) {
            var ast = MentalCaressParsers.ParseProgram(mcsource);
            var bf = CodeGen.FromAST(ast);
            return bf;
        }
    }
}
