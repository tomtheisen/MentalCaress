using System;

namespace BrainfuckRun {
    public class Tape {
        private readonly byte[] Memory = new byte[0x10000];
        private const ushort InitialHead = 0x8000;
        private ushort Head = InitialHead;
        private ushort LeftFrontier = InitialHead;
        private ushort RightFrontier = InitialHead;

        public byte Read() => Memory[Head];
        public void Write(byte v) => Memory[Head] = v;
        public byte Increment(byte by) => Memory[Head] += by;
        public void Move(ushort by) {
            Head += by;
            LeftFrontier = Math.Min(LeftFrontier, Head);
            RightFrontier = Math.Max(RightFrontier, Head);
        }
    }
}
