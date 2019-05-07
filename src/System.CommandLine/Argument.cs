﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine.Binding;
using System.Linq;

namespace System.CommandLine
{
    public class Argument : IArgument
    {
        private Func<object> _defaultValue;
        private readonly List<string> _suggestions = new List<string>();
        private readonly List<ISuggestionSource> _suggestionSources = new List<ISuggestionSource>();
        private IArgumentArity _arity;
        private HashSet<string> _validValues;
        private TryConvertArgument _convertArguments;
        private Symbol _parent;

        public string Name { get; set; }

        public string Description { get; set; }

        public IArgumentArity Arity
        {
            get
            {
                if (_arity == null)
                {
                    if (ArgumentType != null)
                    {
                        return ArgumentArity.Default(ArgumentType, Parent);
                    }
                    else
                    {
                        return ArgumentArity.Zero;
                    }
                }

                return _arity;
            }
            set => _arity = value;
        }

        internal TryConvertArgument ConvertArguments
        {
            get
            {
                if (_convertArguments == null &&
                    ArgumentType != null)
                {
                    if (ArgumentType.CanBeBoundFromScalarValue())
                    {
                        if (Arity.MaximumNumberOfValues == 1 &&
                            ArgumentType == typeof(bool))
                        {
                            _convertArguments = (SymbolResult symbol, out object value) =>
                            {
                                value = ArgumentConverter.Parse(
                                    this,
                                    typeof(bool),
                                    symbol.Tokens.SingleOrDefault()?.Value ?? bool.TrueString);

                                return value is SuccessfulArgumentResult;
                            };
                        }
                        else
                        {
                            _convertArguments = DefaultConvert;
                        }
                    }
                }

                return _convertArguments;

                bool DefaultConvert(SymbolResult symbol, out object value)
                {
                    switch (Arity.MaximumNumberOfValues)
                    {
                        case 1:
                            value = ArgumentConverter.Parse(
                                this,
                                ArgumentType,
                                symbol.Arguments.SingleOrDefault());
                            break;

                        default:
                            value = ArgumentConverter.ParseMany(
                                this,
                                ArgumentType,
                                symbol.Arguments);
                            break;
                    }

                    return value is SuccessfulArgumentResult;
                }
            }
            set => _convertArguments = value;
        }

        public Type ArgumentType { get; set; }

        internal List<ValidateSymbol> SymbolValidators { get; } = new List<ValidateSymbol>();

        public Symbol Parent
        {
            get => _parent;
            internal set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (_parent != null)
                {
                    throw new InvalidOperationException($"{nameof(Parent)} is already set.");
                }

                _parent = value;
            }
        }

        public void AddValidator(ValidateSymbol validator) => SymbolValidators.Add(validator);

        public object GetDefaultValue() => _defaultValue?.Invoke();

        public void SetDefaultValue(object value) => SetDefaultValue(() => value);

        public void SetDefaultValue(Func<object> value) => _defaultValue = value;

        public bool HasDefaultValue => _defaultValue != null;

        public static Argument None => new Argument { Arity = ArgumentArity.Zero };

        public void AddSuggestions(IReadOnlyCollection<string> suggestions)
        {
            if (suggestions == null)
            {
                throw new ArgumentNullException(nameof(suggestions));
            }

            _suggestions.AddRange(suggestions);
        }

        public void AddSuggestionSource(ISuggestionSource suggest)
        {
            if (suggest == null)
            {
                throw new ArgumentNullException(nameof(suggest));
            }

            _suggestionSources.Add(suggest);
        }

        public void AddSuggestionSource(Suggest suggest)
        {
            if (suggest == null)
            {
                throw new ArgumentNullException(nameof(suggest));
            }

            AddSuggestionSource(new AnonymousSuggestionSource(suggest));
        }

        internal void AddValidValues(IEnumerable<string> values)
        {
            if (_validValues == null)
            {
                _validValues = new HashSet<string>();
            }

            _validValues.UnionWith(values);
        }

        public IEnumerable<string> Suggest(string textToMatch)
        {
            var fixedSuggestions = _suggestions;

            var dynamicSuggestions = _suggestionSources
                .SelectMany(source => source.Suggest(textToMatch));

            var typeSuggestions = SuggestionSource.ForType(ArgumentType)
                                                  .Suggest(textToMatch);

            return fixedSuggestions
                   .Concat(dynamicSuggestions)
                   .Concat(typeSuggestions)
                   .Distinct()
                   .OrderBy(c => c)
                   .Containing(textToMatch);
        }

        public override string ToString() => $"{nameof(Argument)}: {Name}";

        internal (ArgumentResult, ParseError) Validate(SymbolResult symbolResult)
        {
            ArgumentResult result = null;

            var error = UnrecognizedArgumentError() ??
                        CustomError();

            if (error == null)
            {
                result = Parse();

                var canTokenBeRetried =
                    symbolResult.Symbol is ICommand ||
                    Arity.MinimumNumberOfValues == 0;

                switch (result)
                {
                    case FailedArgumentArityResult arityFailure:

                        error = new ParseError(arityFailure.ErrorMessage,
                                               symbolResult,
                                               canTokenBeRetried);
                        break;

                    case FailedArgumentTypeConversionResult conversionFailure:

                        error = new ParseError(conversionFailure.ErrorMessage,
                                               symbolResult,
                                               canTokenBeRetried);
                        break;

                    case FailedArgumentResult general:

                        error = new ParseError(general.ErrorMessage,
                                               symbolResult,
                                               false);
                        break;
                }
            }

            return (result, error);

            ArgumentResult Parse()
            {
                var failedResult = ArgumentArity.Validate(symbolResult,
                                                          this,
                                                          Arity.MinimumNumberOfValues,
                                                          Arity.MaximumNumberOfValues);

                if (failedResult != null)
                {
                    return failedResult;
                }

                if (symbolResult.UseDefaultValueFor(this))
                {
                    return ArgumentResult.Success(this, GetDefaultValue());
                }

                if (ConvertArguments != null)
                {
                    var success = ConvertArguments(symbolResult, out var value);

                    if (value is ArgumentResult argumentResult)
                    {
                        return argumentResult;
                    }
                    else if (success)
                    {
                        return ArgumentResult.Success(this, value);
                    }
                    else
                    {
                        return ArgumentResult.Failure(this, symbolResult.ErrorMessage ?? $"Invalid: {symbolResult.Token} {string.Join(" ", symbolResult.Arguments)}");
                    }
                }

                switch (Arity.MaximumNumberOfValues)
                {
                    case 0:
                        return ArgumentResult.Success(this, null);

                    case 1:
                        return ArgumentResult.Success(this, symbolResult.Arguments.SingleOrDefault());

                    default:
                        return ArgumentResult.Success(this, symbolResult.Arguments);
                }
            }

            ParseError UnrecognizedArgumentError()
            {
                if (_validValues?.Count > 0 &&
                    symbolResult.Tokens.Count > 0)
                {
                    foreach (var token in symbolResult.Tokens)
                    {
                        if (!_validValues.Contains(token.Value))
                        {
                            return new ParseError(
                                symbolResult.ValidationMessages
                                            .UnrecognizedArgument(token.Value,
                                                                  _validValues),
                                symbolResult,
                                canTokenBeRetried: false);
                        }
                    }
                }

                return null;
            }

            ParseError CustomError()
            {
                foreach (var symbolValidator in SymbolValidators)
                {
                    var errorMessage = symbolValidator(symbolResult);

                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        return new ParseError(errorMessage, symbolResult, false);
                    }
                }

                return null;
            }
        }

        IArgumentArity IArgument.Arity => Arity;

        Type IValueDescriptor.Type => ArgumentType;
    }
}
