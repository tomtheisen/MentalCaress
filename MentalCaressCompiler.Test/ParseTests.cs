using System;
using Sprache;
using Xunit;
using MentalCaressCompiler;

namespace MentalCaressCompiler.Test {
    public class ParseTests {
        [Fact]
        public void ActionParseTest() {
            var act = MentalCaressParsers.Action1.Parse("readnum x");
            var expected = new AST.Action1("readnum", new("x"));
            Assert.Equal(expected, act);
        }
    }
}
