﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.CommandLine
{
    internal class TokenizeError
    {
        public TokenizeError(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public override string ToString() => Message;
    }
}
