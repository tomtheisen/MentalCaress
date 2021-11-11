using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalCaressCompiler {
    public static class ParserExtensions {
        public static Parser<T> ThenLookAhead<T, U>(this Parser<T> @this, Parser<U> preview) {
            return input => {
                IResult<T> result = @this(input);
                if (!result.WasSuccessful)  return result;

                var previewResult = preview(result.Remainder);
                if (previewResult.WasSuccessful) return result;
                else return Result.Failure<T>(input, previewResult.Message, previewResult.Expectations);
            };
        }

        public static Parser<T> ThenNot<T, U>(this Parser<T> @this, Parser<U> preview)
           => @this.ThenLookAhead(preview.Not());
    }
}
