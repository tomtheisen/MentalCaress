using BrainfuckRun;
using System;
using System.Text;

namespace MentalCaressCompiler.Test {
    class SimulatedTerminal : ITerminal {
        private StringBuilder Output = new();
        private byte[] Input;
        private int Next = 0;

        public SimulatedTerminal(string input = "") {
            Input = Encoding.UTF8.GetBytes(input.Replace("\r", ""));
        }
        
        public byte? Read() => Input[Next++];

        public void Write(byte val) => Output.Append((char)val);

        public string GetOutput() => Output.ToString();
    }
}
