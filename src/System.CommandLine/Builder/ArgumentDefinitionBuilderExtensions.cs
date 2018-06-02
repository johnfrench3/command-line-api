// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.IO;
using System.Linq;

namespace System.CommandLine.Builder
{
    public static class ArgumentDefinitionBuilderExtensions
    {
        #region arity

        public static ArgumentDefinition ExactlyOne(
            this ArgumentDefinitionBuilder builder)
        {
            builder.AddValidator(symbol => {
                var argumentCount = symbol.Arguments.Count;

                if (argumentCount == 0)
                {
                    switch (symbol)
                    {
                        case Command command:
                            return command.ValidationMessages.RequiredArgumentMissingForCommand(command.Definition);
                        case Option option:
                            return symbol.ValidationMessages.RequiredArgumentMissingForOption(option.Definition);
                    }
                }

                if (argumentCount > 1)
                {
                    switch (symbol)
                    {
                        case Command command:
                            return command.ValidationMessages.CommandExpectsOneArgument(command.Definition, command.Arguments.Count);
                        case Option option:
                            return symbol.ValidationMessages.OptionExpectsOneArgument(option.Definition, symbol.Arguments.Count);
                    }
                }

                return null;
            });

            builder.ArgumentArity = ArgumentArity.One;

            return builder.Build();
        }

        public static ArgumentDefinition None(
            this ArgumentDefinitionBuilder builder)
        {
            builder.ArgumentArity = ArgumentArity.Zero;
            builder.SymbolValidators.AddRange(ArgumentDefinition.None.SymbolValidators);
            return builder.Build();
        }

        public static ArgumentDefinition ZeroOrMore(
            this ArgumentDefinitionBuilder builder)
        {
            builder.ArgumentArity = ArgumentArity.Many;

            return builder.Build();
        }

        public static ArgumentDefinition ZeroOrOne(
            this ArgumentDefinitionBuilder builder)
        {
            builder.AddValidator(symbol =>
            {
                if (symbol.Arguments.Count > 1)
                {
                    switch (symbol)
                    {
                        case Command command:
                            return command.ValidationMessages.CommandExpectsOneArgument(command.Definition, command.Arguments.Count);
                        case Option option:
                            return symbol.ValidationMessages.OptionExpectsOneArgument(option.Definition, option.Arguments.Count);
                    }
                }

                return null;
            });

            builder.ArgumentArity = ArgumentArity.One;

            return builder.Build();
        }

        public static ArgumentDefinition OneOrMore(
            this ArgumentDefinitionBuilder builder)
        {
            builder.AddValidator(symbol =>
            {
                var optionCount = symbol.Arguments.Count;

                if (optionCount != 0)
                {
                    return null;
                }

                switch (symbol)
                {
                    case Command command:
                        return command.ValidationMessages.RequiredArgumentMissingForCommand(command.Definition);
                    case Option option:
                        return symbol.ValidationMessages.RequiredArgumentMissingForOption(option.Definition);
                }

                return null;
            });

            builder.ArgumentArity = ArgumentArity.Many;

            return builder.Build();
        }

        #endregion

        #region set inclusion

        public static ArgumentDefinitionBuilder FromAmong(
            this ArgumentDefinitionBuilder builder,
            params string[] values)
        {
            builder.ValidTokens.UnionWith(values);

            builder.SuggestionSource.AddSuggestions(values);

            return builder;
        }

        #endregion

        #region files

        public static ArgumentDefinitionBuilder ExistingFilesOnly(
            this ArgumentDefinitionBuilder builder)
        {
            builder.AddValidator(symbol =>
            {
                return symbol.Arguments
                                   .Where(filePath => !File.Exists(filePath) &&
                                                      !Directory.Exists(filePath))
                                   .Select(symbol.ValidationMessages.FileDoesNotExist)
                                   .FirstOrDefault();
            });
            return builder;
        }

        public static ArgumentDefinitionBuilder LegalFilePathsOnly(
            this ArgumentDefinitionBuilder builder)
        {
            builder.AddValidator(symbol =>
            {
                foreach (var arg in symbol.Arguments)
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

        #region type / return value

        public static ArgumentDefinition ParseArgumentsAs<T>(
            this ArgumentDefinitionBuilder builder) =>
            ParseArgumentsAs(
                builder,
                typeof(T));

        public static ArgumentDefinition ParseArgumentsAs(
            this ArgumentDefinitionBuilder builder,
            Type type) =>
            ParseArgumentsAs(
                builder,
                type,
                symbol => {
                    switch (type.DefaultArity().MaximumNumberOfArguments)
                    {
                        case 1:
                            return ArgumentConverter.Parse(type, symbol.Arguments.SingleOrDefault());
                        default:
                            return ArgumentConverter.ParseMany(type, symbol.Arguments);
                    }
                });

        public static ArgumentDefinition ParseArgumentsAs<T>(
            this ArgumentDefinitionBuilder builder,
            ConvertArgument convert,
            ArgumentArityValidator arity = null) =>
            ParseArgumentsAs(
                builder,
                typeof(T),
                convert,
                arity);

        public static ArgumentDefinition ParseArgumentsAs(
            this ArgumentDefinitionBuilder builder,
            Type type,
            ConvertArgument convert,
            ArgumentArityValidator arity = null)
        {
            if (convert == null)
            {
                throw new ArgumentNullException(nameof(convert));
            }

            arity = arity ?? type.DefaultArity();

            if (arity.MaximumNumberOfArguments == 1)
            {
                var originalConvert = convert;

                if (type == typeof(bool))
                {
                    convert = symbol =>
                        ArgumentConverter.Parse<bool>(symbol.Arguments.SingleOrDefault() ?? "true");
                }
                else
                {
                    convert = symbol => {
                        if (symbol.Arguments.Count != 1)
                        {
                            string message = null;

                            switch (symbol)
                            {
                                case Command command:
                                    message = command.ValidationMessages.CommandExpectsOneArgument(command.Definition, command.Arguments.Count);
                                    break;
                                case Option option:
                                    message = symbol.ValidationMessages.OptionExpectsOneArgument(option.Definition, symbol.Arguments.Count);
                                    break;
                            }

                            return ArgumentParseResult.Failure(message);
                        }

                        return originalConvert(symbol);
                    };
                }
            }

            builder.ArgumentArity = arity;

            builder.ConvertArguments = convert;

            return builder.Build();
        }

        internal static ArgumentArityValidator DefaultArity(this Type type) =>
            typeof(IEnumerable).IsAssignableFrom(type) &&
            type != typeof(string)
                ? ArgumentArity.Many
                : ArgumentArity.One;

        #endregion

        public static ArgumentDefinitionBuilder WithHelp(
            this ArgumentDefinitionBuilder builder,
            string name = null,
            string description = null,
            bool isHidden = ArgumentsRuleHelp.DefaultIsHidden)
        {
            builder.Help = new ArgumentsRuleHelp(name, description, isHidden);

            return builder;
        }

        public static ArgumentDefinitionBuilder WithDefaultValue(
            this ArgumentDefinitionBuilder builder,
            Func<object> defaultValue)
        {
            builder.DefaultValue = defaultValue;

            return builder;
        }

        public static ArgumentDefinitionBuilder AddSuggestions(
            this ArgumentDefinitionBuilder builder,
            params string[] suggestions)
        {
            builder.SuggestionSource.AddSuggestions(suggestions);

            return builder;
        }

        public static ArgumentDefinitionBuilder AddSuggestionSource(
            this ArgumentDefinitionBuilder builder,
            Suggest suggest)
        {
            if (suggest == null)
            {
                throw new ArgumentNullException(nameof(suggest));
            }

            builder.SuggestionSource.AddSuggestionSource(suggest);

            return builder;
        }
    }
}
