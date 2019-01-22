﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;

namespace System.CommandLine.Rendering
{
    public static class CommandLineBuilderExtensions
    {
        public static CommandLineBuilder UseAnsiTerminalWhenAvailable(
            this CommandLineBuilder builder)
        {
            builder.ConfigureConsole(context =>
            {
                var console = context.Console;

                var terminal = console.GetTerminal(
                    PreferVirtualTerminal(context),
                    OutputMode(context));

                return terminal ?? console;
            });

            return builder;
        }

        internal static bool PreferVirtualTerminal(
            this InvocationContext context)
        {
            if (context.ParseResult.Directives.TryGetValues(
                    "enable-vt",
                    out var pvtString) &&
                bool.TryParse(
                    pvtString.FirstOrDefault(),
                    out var pvt))
            {
                return pvt;
            }

            return true;
        }

        public static OutputMode OutputMode(this InvocationContext context)
        {
            if (context.ParseResult.Directives.TryGetValues(
                    "output",
                    out var modeString) &&
                Enum.TryParse<OutputMode>(
                    modeString.FirstOrDefault(),
                    true,
                    out var mode))
            {
                return mode;
            }

            return context.Console.DetectOutputMode();
        }
    }
}
