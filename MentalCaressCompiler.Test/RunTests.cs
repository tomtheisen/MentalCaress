using BrainfuckRun;
using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MentalCaressCompiler.Test {
    public class RunTests {
        private string RunMentalCaress(string source, string input = "") {
            var mcast = MentalCaressParsers.Program.Parse(source);
            var bf = CodeGen.FromAST(mcast);
            var bfast = BrainfuckParse.Parse(bf);
            Tape tape = new();
            SimulatedTerminal io = new (input);
            bfast.Run(tape, io);
            return io.GetOutput();
        }

        [Fact]
        public void BasicOutputTest() {
            string source = "var x='a'\nwrite x\nx=x+1\nwrite x";
            var output = RunMentalCaress(source);
            Assert.Equal("ab", output);
        }

        [Fact]
        public void LeapTest() {
            string filename = Path.Combine(Environment.CurrentDirectory, "../../../..", "Programs", "leapyears.mc");
            string mcsource = File.ReadAllText(filename);
            string output = RunMentalCaress(mcsource);
            string expected = string.Concat( 
                from year in Enumerable.Range(1800, 601)
                where year % 4 == 0
                where year % 400 == 0 || year % 100 > 0
                select year + "\n");
            Assert.Equal(expected, output);
        }
    }
}
