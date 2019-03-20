﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace System.CommandLine.Binding
{
    public class PropertyDescriptor : IValueDescriptor
    {
        private readonly PropertyInfo _propertyInfo;

        internal PropertyDescriptor(
            PropertyInfo propertyInfo,
            ModelDescriptor parent)
        {
            Parent = parent;
            _propertyInfo = propertyInfo;
        }

        public string Name => _propertyInfo.Name;

        public ModelDescriptor Parent { get; }

        internal string Path => Parent != null
                                    ? Parent + "." + Name
                                    : Name;

        public Type Type => _propertyInfo.PropertyType;

        public bool HasDefaultValue => false;

        public object GetDefaultValue() => Type.GetDefaultValueForType();

        public void SetValue(object instance, object value)
        {
            _propertyInfo.SetValue(instance, value);
        }

        public override string ToString() => $"{Type.Name} {Path}";
    }
}
