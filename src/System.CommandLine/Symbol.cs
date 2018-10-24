﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace System.CommandLine
{
    public abstract class Symbol : ISymbol
    {
        private readonly HashSet<string> _aliases = new HashSet<string>();
        private readonly HashSet<string> _rawAliases = new HashSet<string>();
        private string _longestAlias = "";
        private string _specifiedName;

        protected internal Symbol(
            IReadOnlyCollection<string> aliases,
            string description,
            Argument argument = null,
            HelpDetail help = null)
        {
            if (aliases == null)
            {
                throw new ArgumentNullException(nameof(aliases));
            }

            if (!aliases.Any())
            {
                throw new ArgumentException("An option must have at least one alias.");
            }

            foreach (var alias in aliases)
            {
                AddAlias(alias);
            }

            Description = description;

            Argument = argument ?? Argument.None;

            Help = help ?? new HelpDetail(Name, Description, false);
        }

        public IReadOnlyCollection<string> Aliases => _aliases;

        public IReadOnlyCollection<string> RawAliases => _rawAliases;

        public Argument Argument { get; protected set; }

        public string Description { get; }

        public HelpDetail Help { get; }

        public string Name
        {
            get => _specifiedName ?? _longestAlias;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
                }

                if (value.Length != value.RemovePrefix().Length)
                {
                    throw new ArgumentException($"Property {GetType().Name}.{nameof(Name)} cannot have a prefix.");
                }

                _specifiedName = value;
            }
        }

        public Command Parent { get; internal set; }

        public SymbolSet Children { get; } = new SymbolSet();

        public void AddAlias(string alias)
        {
            var unprefixedAlias = alias?.RemovePrefix();

            if (string.IsNullOrWhiteSpace(unprefixedAlias))
            {
                throw new ArgumentException("An alias cannot be null, empty, or consist entirely of whitespace.");
            }

            _rawAliases.Add(alias);
            _aliases.Add(unprefixedAlias);

            if (unprefixedAlias.Length > Name?.Length)
            {
                _longestAlias = unprefixedAlias;
            }
        }

        public bool HasAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(alias));
            }

            return _aliases.Contains(alias.RemovePrefix());
        }

        public bool HasRawAlias(string alias) => _rawAliases.Contains(alias);

        public virtual IEnumerable<string> Suggest(
            ParseResult parseResult,
            int? position = null)
        {
            var symbolAliases = Children.Where<ISymbol>(symbol => !symbol.IsHidden())
                                       .SelectMany(symbol => symbol.RawAliases);

            var argumentSuggestions = Argument
                                      .SuggestionSource
                                      .Suggest(parseResult, position);

            return symbolAliases.Concat(argumentSuggestions)
                                .Distinct()
                                .OrderBy(symbol => symbol)
                                .Containing(parseResult.TextToMatch());
        }

        public override string ToString() => $"{GetType().Name}: {Name}";

        ICommand ISymbol.Parent => Parent;

        ISymbolSet ISymbol.Children => Children;
    }
}
