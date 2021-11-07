using BrainfuckRun;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MentalCaressCompiler.Test {
    public class RunTests {
        [Fact]
        public void BasicOutputTest() {
            string source = "var x='a'\nwrite x\nx=x+1\nwrite x";
            var mcast = MentalCaressParsers.Program.Parse(source);
            var bf = CodeGen.FromAST(mcast);
            var bfast = BrainfuckParse.Parse(bf);
            
            Tape tape = new();
            SimulatedTerminal io = new ();
            bfast.Run(tape, io);
            string output = io.GetOutput();

            Assert.Equal("ab", output);
        }
    }
}
