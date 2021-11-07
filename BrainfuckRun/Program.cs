using System;
using System.Collections.Generic;
using System.IO;

namespace BrainfuckRun {
    class Program {
        static void Main(string[] args) {
            string filename = args[0];
            try {
                string source = File.ReadAllText(filename);
                var ast = BrainfuckParse.Parse(source);
                ast.Run(new (), new Terminal());
            }
            catch (IOException ex) {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
