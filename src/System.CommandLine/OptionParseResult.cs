// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.CommandLine
{
    public class OptionParseResult : ParseResult
    {
        internal OptionParseResult(
            IReadOnlyCollection<string> tokens,
            ParsedSymbolSet parsedOptions,
            ParserConfiguration configuration,
            IReadOnlyCollection<string> unparsedTokens = null,
            IReadOnlyCollection<string> unmatchedTokens = null,
            IReadOnlyCollection<ParseError> errors = null,
            string rawInput = null) : base(tokens, parsedOptions, configuration, unparsedTokens, unmatchedTokens, errors, rawInput)
        {
        }

        public ParsedOption this[string alias] => (ParsedOption) ParsedSymbols[alias];
    }
}
