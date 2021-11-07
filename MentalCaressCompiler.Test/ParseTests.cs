using System;
using Sprache;
using Xunit;
using MentalCaressCompiler;
using System.Linq;

namespace MentalCaressCompiler.Test {
    public class ParseTests {
        [Fact]
        public void ActionParseTest() {
            var act = MentalCaressParsers.Action1.Parse("readnum x");
            var expected = new AST.Action1("readnum", new("x"));
            Assert.Equal(expected, act);
        }

        [Fact]
        public void IfNotParseTest() {
            var source = @"ifnot working {
                }";
            var prog = MentalCaressParsers.Block.Parse(source);
        }

        [Fact]
        public void LeapFragment1Test() {
            var source = @"
                ifnot working {
                    y2 = 0
                    y1 = y1 + 1
                }";
            var prog = MentalCaressParsers.Program.Parse(source);
            Assert.NotNull(prog);
        }

        [Fact]
        public void LeapFragment2Test() {
            var source = @"
                ifnot working {
                    y3 = 0
                    y2 = y2 + 1
                    working = y2 - 10
                    ifnot working {
                        y2 = 0
                        y1 = y1 + 1
                    }
                }";
            var prog = MentalCaressParsers.Program.Parse(source);
            Assert.NotNull(prog);
        }

        [Fact]
        public void LeapFullTest() {
            var source = @"
                var y1 = 1
                var y2 = 8
                var y3 = 0
                var y4 = 0

                var working = 1
                loop working {
                    # adding 4 to y4
                    y4 = y4 + 4
                    working = y4 / 10
                    # testing carry
                    if working {
                        y4 = y4 - 10
                        y3 = y3 + 1
                        working = y3 - 10
                        ifnot working {
                            y3 = 0
                            y2 = y2 + 1
                            working = y2 - 10
                            ifnot working {
                                y2 = 0
                                y1 = y1 + 1
                            }
                        }
                    }

                    y1 = y1 + 48
                    write y1
                    y1 = y1 - 48

                    y2 = y2 + 48
                    write y2
                    y2 = y2 - 48

                    y3 = y3 + 48
                    write y3
                    y3 = y3 - 48

                    y4 = y4 + 48
                    write y4
                    y4 = y4 - 48

                    writeline

                    working = y2 - 4
                }";
            var prog = MentalCaressParsers.Program.Parse(source);
            string bf = CodeGen.FromAST(prog);
            Assert.NotNull(bf);
        }

    }
}
