﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public class UseExceptionHandlerTests
    {
        private readonly TestConsole _console = new TestConsole();

        [Fact]
        public async Task Declaration_of_UseExceptionHandler_can_come_after_other_middleware()
        {
            await new CommandLineBuilder()
                  .AddCommand("the-command")
                  .UseMiddleware(_ => throw new Exception("oops!"))
                  .UseExceptionHandler()
                  .Build()
                  .InvokeAsync("the-command", _console);

            _console.Error
                    .ToString()
                    .Should()
                    .Contain("oops!");
        }

        [Fact]
        public async Task UseExceptionHandler_catches_middleware_exceptions_and_writes_details_to_standard_error()
        {
            var parser = new CommandLineBuilder()
                         .AddCommand("the-command")
                         .UseMiddleware(_ => throw new Exception("oops!"))
                         .UseExceptionHandler()
                         .Build();

            var resultCode = await parser.InvokeAsync("the-command", _console);

            _console.Error.ToString().Should().Contain("Unhandled exception: System.Exception: oops!");

            resultCode.Should().Be(1);
        }

        [Fact]
        public async Task UseExceptionHandler_catches_command_handler_exceptions_and_sets_result_code_to_1()
        {
            var command = new Command("the-command");
            command.Handler = CommandHandler.Create(() => throw new Exception("oops!"));

            var parser = new CommandLineBuilder()
                         .AddCommand(command)
                         .UseExceptionHandler()
                         .Build();

            var resultCode = await parser.InvokeAsync("the-command", _console);

            resultCode.Should().Be(1);
        }

        [Fact]
        public async Task UseExceptionHandler_catches_command_handler_exceptions_and_writes_details_to_standard_error()
        {
            var command = new Command("the-command");
            command.Handler = CommandHandler.Create(() => throw new Exception("oops!"));

            var parser = new CommandLineBuilder()
                         .AddCommand(command)
                         .UseExceptionHandler()
                         .Build();

            await parser.InvokeAsync("the-command", _console);

            _console.Error.ToString().Should().Contain("System.Exception: oops!");
        }

        [Fact]
        public async Task Declaration_of_UseExceptionHandler_can_come_before_other_middleware()
        {
            await new CommandLineBuilder()
                  .AddCommand("the-command")
                  .UseExceptionHandler()
                  .UseMiddleware(_ => throw new Exception("oops!"))
                  .Build()
                  .InvokeAsync("the-command", _console);

            _console.Error
                    .ToString()
                    .Should()
                    .Contain("oops!");
        }
    }
}
