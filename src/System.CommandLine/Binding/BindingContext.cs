﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Invocation;
using System.Linq;

namespace System.CommandLine.Binding
{
    public sealed class BindingContext
    {
        private IConsole _console;

        public BindingContext(
            ParseResult parseResult,
            IConsole console = null)
        {
            _console = console ?? new SystemConsole();

            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            ServiceProvider = new ServiceProvider(this);
        }

        public ParseResult ParseResult { get; set; }

        internal IConsoleFactory ConsoleFactory { get; set; }

        internal IHelpBuilder HelpBuilder => (IHelpBuilder)ServiceProvider.GetService(typeof(IHelpBuilder));

        public IConsole Console
        {
            get
            {
                if (ConsoleFactory != null)
                {
                    var consoleFactory = ConsoleFactory;
                    ConsoleFactory = null;
                    _console = consoleFactory.CreateConsole(this);
                }

                return _console;
            }
        }

        internal ServiceProvider ServiceProvider { get; }

        public void AddService(Type serviceType, Func<object> factory)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            ServiceProvider.AddService(serviceType, factory);
        }

        internal bool TryGetValueSource(
            IValueDescriptor valueDescriptor,
            out IValueSource valueSource)
        {
            foreach (var symbol in ParseResult.ValueDescriptors())
            {
                if (ValueDescriptor.CanBind(
                    from: symbol, 
                    to: valueDescriptor))
                {
                    valueSource = new SymbolValueSource((ISymbol)symbol);

                    return true;
                }
            }

            if (ServiceProvider.AvailableServiceTypes.Contains(valueDescriptor.Type))
            {
                valueSource = new ServiceProviderValueSource();
                return true;
            }

            valueSource = null;
            return false;
        }

        internal bool TryBind(
            IValueDescriptor valueDescriptor,
            IValueSource valueSource,
            out BoundValue boundValue)
        {
            if (valueSource.TryGetValue(valueDescriptor, this, out var value))
            {
                boundValue = new BoundValue(value, valueDescriptor, valueSource);
                return true;
            }
            else
            {
                boundValue = null;
                return false;
            }
        }
    }
}
