using System;
using System.IO;
using System.Linq;

namespace MentalCaressCompiler {
    class Program {
        static void Main(string[] args) {
            string inFile = args.Single(a => !a.StartsWith('-'));
            string? outFile = args.SingleOrDefault(a => a.StartsWith("-out="))?[5..];
            bool comments = args.Contains("-c");
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

		static string Compile(string mcsource, bool comments) {
            var ast = MentalCaressParsers.ParseProgram(mcsource);
            var bf = CodeGen.FromAST(ast, comments);
            return bf;
        }
    }
}
