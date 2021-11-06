using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfuckRun {
    interface ITerminal {
        void Write(byte val);
        byte? Read();
    }

    class Terminal : ITerminal {
        public byte? Read() => Console.Read() switch {
            int r when r < 0 => null,
            13 => this.Read(),  // swallow \r
            int r => (byte)r,
        };

        public void Write(byte val) => Console.Write((char)val);
    }
}
