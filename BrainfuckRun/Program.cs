using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrainfuckRun {
    class Program {
        static void Main(string[] args) {
            string filename = args.Last();
            bool optimize = args.Contains("-o");
            try {
                string source = File.ReadAllText(filename);
                var ast = BrainfuckParse.Parse(source);
                ast.Run(new (), new Terminal());
                if (optimize) DoOptimize(ast);
            }
            catch (IOException ex) {
                Console.Error.WriteLine(ex.Message);
            }
        }

        static void DoOptimize(BrainfuckProgram ast) {
            StringBuilder opt = new();
            ast.Serialize(opt, optimize: true);
            Console.WriteLine();
            Console.WriteLine("Optimization attempt:");
            Console.WriteLine("Optimized size: {0}", opt.Length);
            Console.WriteLine(opt);
        }
    }
}
