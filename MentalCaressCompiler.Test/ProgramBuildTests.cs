using BrainfuckRun;
using Sprache;
using System;
using Xunit;

namespace MentalCaressCompiler.Test {
    public class ProgramBuildTests {
        [Fact]
        public void ScopeTest() {
            var mcast = MentalCaressParsers.Program.Parse(@"
				var outer = 1
				loop outer {
					outer -= 1
					var inner = 1
					loop inner {
						inner -= 1
					}
				}

				outer = 1
				loop outer {
					outer -= 1
					var inner = 1
					loop inner {
						inner -= 1
					}
				}");
			var bf = CodeGen.FromAST(mcast);
			Assert.NotNull(bf);
        }
    }
}
