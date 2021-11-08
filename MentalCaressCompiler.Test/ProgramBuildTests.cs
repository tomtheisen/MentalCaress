using BrainfuckRun;
using Sprache;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MentalCaressCompiler.Test {
    public class ProgramBuildTests {
        [Fact]
        public void BuilderTest1() {
            ProgramBuilder builder = new();
            builder.Allocate(out int x, 0, nameof(x));
            builder.AllocateAndCopy(out int y, x, nameof(y));
            builder.AllocateAndCopy(out int z, x, nameof(z));
        }
    }
}
