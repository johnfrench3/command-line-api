﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Binding;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests.Binding
{
    public class ModelBinderTests
    {
        [Fact]
        public void Option_arguments_are_bound_by_name_to_constructor_parameters()
        {
            var argument = new Argument<string>("the default");

            var option = new Option("--string-option",
                                    argument: argument);

            var command = new Command("the-command");
            command.AddOption(option);
            var binder = new ModelBinder<ClassWithMultiLetterCtorParameters>();

            var parser = new Parser(command);
            var bindingContext = new BindingContext(
                parser.Parse("--string-option not-the-default"));

            var instance = (ClassWithMultiLetterCtorParameters)binder.CreateInstance(bindingContext);

            instance.StringOption.Should().Be("not-the-default");
        }

        [Theory]
        [InlineData(typeof(string), "hello", "hello")]
        [InlineData(typeof(int), "123", 123)]
        public void Command_arguments_are_bound_by_name_to_constructor_parameters(
            Type type,
            string commandLine,
            object expectedValue)
        {
            var targetType = typeof(ClassWithCtorParameter<>).MakeGenericType(type);
            var binder = new ModelBinder(targetType);

            var command = new Command("the-command")
                          {
                              Argument = new Argument
                                         {
                                             Name = "value",
                                             ArgumentType = type
                                         }
                          };
            var parser = new Parser(command);

            var bindingContext = new BindingContext(parser.Parse(commandLine));

            var instance = binder.CreateInstance(bindingContext);

            object valueReceivedValue = ((dynamic)instance).Value;

            valueReceivedValue.Should().Be(expectedValue);
        }

        [Fact]
        public void Explicitly_configured_default_values_can_be_bound_to_constructor_parameters()
        {
            var argument = new Argument<string>("the default");

            var option = new Option("--string-option",
                                    argument: argument);

            var command = new Command("the-command");
            command.AddOption(option);
            var binder = new ModelBinder(typeof(ClassWithMultiLetterCtorParameters));

            var parser = new Parser(command);
            var bindingContext = new BindingContext(parser.Parse(""));

            var instance = (ClassWithMultiLetterCtorParameters)binder.CreateInstance(bindingContext);

            instance.StringOption.Should().Be("the default");
        }

        [Fact]
        public void Option_arguments_are_bound_by_name_to_property_setters()
        {
            var argument = new Argument<bool>();

            var option = new Option("--bool-option",
                                    argument: argument);

            var command = new Command("the-command");
            command.AddOption(option);
            var binder = new ModelBinder(typeof(ClassWithMultiLetterSetters));

            var parser = new Parser(command);
            var invocationContext = new BindingContext(
                parser.Parse("--bool-option"));

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(invocationContext);

            instance.BoolOption.Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(string), "hello", "hello")]
        [InlineData(typeof(int), "123", 123)]
        public void Command_arguments_are_bound_by_name_to_property_setters(
            Type type,
            string commandLine,
            object expectedValue)
        {
            var targetType = typeof(ClassWithSetter<>).MakeGenericType(type);
            var binder = new ModelBinder(targetType);

            var command = new Command("the-command")
                          {
                              Argument = new Argument
                                         {
                                             Name = "value",
                                             ArgumentType = type
                                         }
                          };
            var parser = new Parser(command);

            var bindingContext = new BindingContext(parser.Parse(commandLine));

            var instance = binder.CreateInstance(bindingContext);

            object valueReceivedValue = ((dynamic)instance).Value;

            valueReceivedValue.Should().Be(expectedValue);
        }

        [Fact]
        public void Explicitly_configured_default_values_can_be_bound_to_property_setters()
        {
            var argument = new Argument<string>("the default");

            var option = new Option("--string-option",
                                    argument: argument);

            var command = new Command("the-command");
            command.AddOption(option);
            var binder = new ModelBinder(typeof(ClassWithMultiLetterSetters));

            var parser = new Parser(command);
            var bindingContext = new BindingContext(
                parser.Parse(""));

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(bindingContext);

            instance.StringOption.Should().Be("the default");
        }

        [Fact]
        public void Property_setters_with_no_default_value_and_no_matching_option_are_not_called()
        {
            var command = new Command("the-command")
                          {
                              new Option("-s", argument: new Argument<string>("the default")),
                              new Option("--string-option", argument: new Argument<string>())
                          };

            var binder = new ModelBinder(typeof(ClassWithSettersAndCtorParametersWithDifferentNames));

            var parser = new Parser(command);
            var bindingContext = new BindingContext(
                parser.Parse(""));

            var instance = (ClassWithSettersAndCtorParametersWithDifferentNames)binder.CreateInstance(bindingContext);

            instance.StringOption.Should().Be("the default");
        }

        [Fact]
        public void Parse_result_can_be_used_to_create_an_instance_without_doing_handler_invocation()
        {
            var parser = new Parser(new Command("the-command")
                                    {
                                        new Option("--int-option")
                                        {
                                            Argument = new Argument<int>()
                                        }
                                    });
            var descriptor = ModelDescriptor.FromType<ClassWithMultiLetterSetters>();
            var bindingContext = new BindingContext(parser.Parse("the-command --int-option 123"));
            var binder = new ModelBinder(descriptor);

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(bindingContext);

            instance.IntOption.Should().Be(123);
        }

        [Fact]
        public void Parse_result_can_be_used_to_modify_an_existing_instance_without_doing_handler_invocation()
        {
            var parser = new Parser(new Command("the-command")
                                    {
                                        new Option("--int-option")
                                        {
                                            Argument = new Argument<int>()
                                        }
                                    });
            var descriptor = ModelDescriptor.FromType<ClassWithMultiLetterSetters>();
            var instance = new ClassWithMultiLetterSetters();
            var bindingContext = new BindingContext(parser.Parse("the-command --int-option 123"));
            var binder = new ModelBinder(descriptor);

            binder.UpdateInstance(instance, bindingContext);

            instance.IntOption.Should().Be(123);
        }

        [Fact]
        public void Values_from_parent_options_on_parent_commands_can_be_bound()
        {
            var childCommand = new Command("child-command");
            var option = new Option("--int-option")
                         {
                             Argument = new Argument<int>()
                         };
            var parentCommand = new Command("parent-command")
                                {
                                    option,
                                    childCommand
                                };

            var binder = new ModelBinder<ClassWithMultiLetterSetters>();

            binder.BindMemberFromOption(
                c => c.IntOption,
                option);

            var bindingContext = new BindingContext(parentCommand.Parse("parent-command --int-option 123 child-command"));

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(bindingContext);

            instance.IntOption.Should().Be(123);
        }

        [Fact]
        public void Values_from_parent_parent_commands_can_be_bound()
        {
            var childCommand = new Command("child-command");

            var parentCommand = new Command("parent-command", argument: new Argument<int>())
                                {
                                    childCommand
                                };

            var binder = new ModelBinder<ClassWithMultiLetterSetters>();

            binder.BindMemberFromCommand(
                c => c.IntOption,
                parentCommand);

            var bindingContext = new BindingContext(parentCommand.Parse("parent-command 123 child-command"));

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(bindingContext);

            instance.IntOption.Should().Be(123);
        }

        [Fact]
        public void Arbitrary_values_can_be_bound()
        {
            var command = new Command("the-command");

            var binder = new ModelBinder<ClassWithMultiLetterSetters>();

            binder.BindMemberFromValue(
                c => c.IntOption,
                _ => 123);

            var bindingContext = new BindingContext(command.Parse("the-command"));

            var instance = (ClassWithMultiLetterSetters)binder.CreateInstance(bindingContext);

            instance.IntOption.Should().Be(123);
        }
    }
}
