﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.CommandLine
{
    internal static class ConsoleExtensions
    {
        internal static void SetTerminalForegroundRed(this IConsole console)
        {
            if (console.GetType().GetInterfaces().Any(i => i.Name == "ITerminal"))
            {
                ((dynamic)console).ForegroundColor = ConsoleColor.Red;
            }

            var supported = Platform.SupportsOperation(() => Console.IsOutputRedirected,
                    out var isOutputRedirected);
            if (!supported)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (!isOutputRedirected)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
        }

        internal static void ResetTerminalForegroundColor(this IConsole console)
        {
            if (console.GetType().GetInterfaces().Any(i => i.Name == "ITerminal"))
            {
                ((dynamic)console).ForegroundColor = ConsoleColor.Red;
            }

            var supported = Platform.SupportsOperation(() => Console.IsOutputRedirected,
                out var isOutputRedirected);
            if (!supported)
            {
                Console.ResetColor();
            }
            else if (!isOutputRedirected)
            {
                Console.ResetColor();
            }
        }
    }
}
