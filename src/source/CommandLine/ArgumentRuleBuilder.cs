﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.DotNet.Cli.CommandLine.ValidationMessages;

namespace Microsoft.DotNet.Cli.CommandLine
{
    public class ArgumentRuleBuilder
    {
        private readonly List<Validate> validators = new List<Validate>();

        public ArgumentParser ArgumentParser { get; set; }

        public ArgumentsRuleHelp Help { get; set; }

        public Func<string> DefaultValue { get; set; }

        public void AddValidator(Validate validator)
        {
            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }


            validators.Add(validator);
        }

        private string Validate(ParsedSymbol parsedOption)
        {
            if (parsedOption == null)
            {
                throw new ArgumentNullException(nameof(parsedOption));
            }

            return validators.Select(v => v(parsedOption))
                             .FirstOrDefault(e => e != null);
        }

        internal ArgumentsRule Build()
        {
            return new ArgumentsRule(ArgumentParser, DefaultValue, Help);
        }
    }

    public static class Define
    {
        public static ArgumentRuleBuilder Arguments()
        {
            return new ArgumentRuleBuilder();
        }

        #region arity

        public static ArgumentsRule None(
            this ArgumentRuleBuilder builder,
            Func<ParsedSymbol, string> errorMessage = null)
        {
            builder.AddValidator(o =>
            {
                if (!o.Arguments.Any())
                {
                    return null;
                }

                if (errorMessage == null)
                {
                    return NoArgumentsAllowed(o.Symbol.ToString());
                }
                else
                {
                    return errorMessage(o);
                }
            });

            return builder.Build();
        }

        public static ArgumentsRule ExactlyOne(
            this ArgumentRuleBuilder builder,
            Func<ParsedSymbol, string> errorMessage = null)
        {
            builder.AddValidator(o =>
            {
                var argumentCount = o.Arguments.Count;

                if (argumentCount == 0)
                {
                    if (errorMessage == null)
                    {
                        return o.Symbol is Command
                                   ? RequiredArgumentMissingForCommand(o.Symbol.ToString())
                                   : RequiredArgumentMissingForOption(o.Symbol.ToString());
                    }
                    else
                    {
                        return errorMessage(o);
                    }
                }

                if (argumentCount > 1)
                {
                    if (errorMessage == null)
                    {
                        return o.Symbol is Command
                                   ? CommandAcceptsOnlyOneArgument(o.Symbol.ToString(), argumentCount)
                                   : OptionAcceptsOnlyOneArgument(o.Symbol.ToString(), argumentCount);
                    }
                    else
                    {
                        return errorMessage(o);
                    }
                }

                return null;
            });

            return builder.Build();
        }

        public static ArgumentsRule ZeroOrMore(
            this ArgumentRuleBuilder builder,
            Func<ParsedOption, string> errorMessage = null)
        {
            return builder.Build();
        }

        public static ArgumentsRule ZeroOrOne(
            this ArgumentRuleBuilder builder,
            Func<ParsedOption, string> errorMessage = null)
        {
            builder.AddValidator(o =>
            {
                if (o.Arguments.Count > 1)
                {
                    return o.Symbol is Command
                               ? CommandAcceptsOnlyOneArgument(o.Symbol.ToString(), o.Arguments.Count)
                               : OptionAcceptsOnlyOneArgument(o.Symbol.ToString(), o.Arguments.Count);
                }

                return null;
            });
            return builder.Build();
        }

        public static ArgumentsRule OneOrMore(
            this ArgumentRuleBuilder builder,
            Func<ParsedSymbol, string> errorMessage = null)
        {
            builder.AddValidator(o =>
            {
                var optionCount = o.Arguments.Count;

                if (optionCount != 0)
                {
                    return null;
                }

                if (errorMessage != null)
                {
                    return errorMessage(o);
                }

                return
                    o.Symbol is Command
                        ? RequiredArgumentMissingForCommand(o.Symbol.ToString())
                        : RequiredArgumentMissingForOption(o.Symbol.ToString());
            });
            return builder.Build();
        }

        public static ArgumentsRule ExactlyOneChild(
            this ArgumentRuleBuilder builder,
            Func<ParsedSymbol, string> errorMessage = null)
        {
            builder.AddValidator(o =>
            {
                var optionCount = o.Children.Count;

                if (optionCount == 0)
                {
                    if (errorMessage == null)
                    {
                        return RequiredArgumentMissingForCommand(o.Symbol.ToString());
                    }
                    else
                    {
                        return errorMessage(o);
                    }
                }

                if (optionCount > 1)
                {
                    if (errorMessage == null)
                    {
                        return CommandAcceptsOnlyOneSubcommand(
                            o.Symbol.ToString(),
                            string.Join(", ", o.Children.Select(a => a.Symbol)));
                    }
                    else
                    {
                        return errorMessage(o);
                    }
                }

                return null;
            });
            return builder.Build();
        }

        public static ArgumentsRule And(this ArgumentRuleBuilder builder,
            ArgumentsRule rule)
        {
            builder.None()
        }

        #endregion

        #region set inclusion

        public static ArgumentRuleBuilder FromAmong(
            this ArgumentRuleBuilder builder,
            params string[] values)
        {
            builder.AddValidator(o =>
            {
                if (o.Arguments.Count == 0)
                {
                    return null;
                }

                var arg = o.Arguments.Single();

                return !values.Contains(arg, StringComparer.OrdinalIgnoreCase)
                           ? UnrecognizedArgument(arg, values)
                           : "";
            });

            return builder;
        }

        #endregion

        #region files

        public static ArgumentRuleBuilder ExistingFilesOnly(
            this ArgumentRuleBuilder builder)
        {
            builder.AddValidator(o => o.Arguments
                                       .Where(filePath => !File.Exists(filePath) &&
                                                          !Directory.Exists(filePath))
                                       .Select(FileDoesNotExist)
                                       .FirstOrDefault());
            return builder;
        }

        public static ArgumentRuleBuilder LegalFilePathsOnly(
            this ArgumentRuleBuilder builder)
        {
            builder.AddValidator(o =>
            {
                foreach (var arg in o.Arguments)
                {
                    try
                    {
                        var fileInfo = new FileInfo(arg);
                    }
                    catch (NotSupportedException ex)
                    {
                        return ex.Message;
                    }
                    catch (ArgumentException ex)
                    {
                        return ex.Message;
                    }
                }

                return null;
            });

            return builder;
        }

        #endregion

        #region type

        public static ArgumentRuleBuilder OfType<T>(
            this ArgumentRuleBuilder builder,
            TypeConversion parse)
        {
            builder.ArgumentParser = new ArgumentParser<T>(parse);

            return builder;
        }

        #endregion

        public static ArgumentRuleBuilder WithHelp(this ArgumentRuleBuilder builder,
            string name = null, string description = null)
        {
            builder.Help = new ArgumentsRuleHelp(name, description);
            return builder;
        }

        public static ArgumentRuleBuilder WithDefaultValue(this ArgumentRuleBuilder builder,
            Func<string> defaultValue)
        {
            builder.DefaultValue = defaultValue;
            return builder;
        }

        public static ArgumentRuleBuilder Validate(
            this ArgumentRuleBuilder builder,
            Validate validate)
        {
            builder.AddValidator(validate);

            return builder;
        }

        public static ArgumentRuleBuilder WithSuggestions(this ArgumentRuleBuilder builder,
            params string[] suggestions)
        {
            builder.ArgumentParser.AddSuggetions((_, __) => suggestions);
            return builder;
        }
    }

    public delegate Result TypeConverter(ParsedSymbol symbol);

    public delegate IEnumerable<string> SuggestionSource(ParseResult parseResult, int? position);

    public abstract class ArgumentParser
    {
        private readonly List<SuggestionSource> suggestionSources = new List<SuggestionSource>();

        public void AddSuggetions(SuggestionSource suggestionSource)
        {
            suggestionSources.Add(suggestionSource);
        }

        public virtual IEnumerable<string> Suggest(
            ParseResult parseResult,
            int? position = null)
        {
            foreach (SuggestionSource suggestionSource in suggestionSources)
            {
                foreach (string suggestion in suggestionSource(parseResult, position))
                {
                    yield return suggestion;
                }
            }
        }


        //public abstract Result Parse(string value);
        public abstract Result Parse(ParsedSymbol value);
    }

    public delegate Result Validate<in T>(T value, ParsedSymbol symbol);

    public delegate Result TypeConversion(ParsedSymbol symbol);

    public class ArgumentParser<T> : ArgumentParser
    {
        private readonly List<Validate<T>> validations = new List<Validate<T>>();
        private readonly TypeConversion typeConversion;

        public ArgumentParser(TypeConversion typeConversion)
        {
            this.typeConversion = typeConversion ??
                         throw new ArgumentNullException(nameof(typeConversion));
        }

        public void AddValidator(Validate<T> validator)
        {
            validations.Add(validator);
        }

        private Result Parse(T value, ParsedSymbol symbol)
        {
            Result result = null;
            foreach (Validate<T> validator in validations)
            {
                result = validator(value, symbol);
                if (result is SuccessfulResult<T> successResult)
                {
                    value = successResult.Value;
                }
                else
                {
                    return result;
                }
            }
            return result ?? Result.Success(value);
        }

        //string -> parsed symbol -> custom type conversion -> type checking -> custom validation

        //public override Result Parse(string value)
        //{
        //    
        //    return parse(value);
        //}

        public override Result Parse(ParsedSymbol symbol)
        {
            Result typeResult = typeConversion(symbol);
            if (typeResult is SuccessfulResult<T> successfulResult)
            {
                return Parse(successfulResult.Value, symbol);
            }
            return typeResult;
        }
    }

    public class SuccessfulResult<T> : Result
    {
        public SuccessfulResult(T value = default(T))
        {
            Value = value;
        }

        public T Value { get; }

        public override bool Successful { get; } = true;
    }

    public class FailedResult : Result
    {
        public string Error { get; }

        public FailedResult(string error)
        {
            Error = error;
        }

        public override bool Successful { get; } = false;
    }

    public abstract class Result
    {
        public abstract bool Successful { get; }

        public static FailedResult Failure(string error) => new FailedResult(error);

        public static SuccessfulResult<T> Success<T>(T value) => new SuccessfulResult<T>(value);
    }
}
