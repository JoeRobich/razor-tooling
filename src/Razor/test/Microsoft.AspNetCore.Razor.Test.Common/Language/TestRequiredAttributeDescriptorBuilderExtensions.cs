﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestRequiredAttributeDescriptorBuilderExtensions
    {
        public static RequiredAttributeDescriptorBuilder Name(this RequiredAttributeDescriptorBuilder builder, string name)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Name = name;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder NameComparisonMode(
            this RequiredAttributeDescriptorBuilder builder,
            RequiredAttributeDescriptor.NameComparisonMode nameComparison)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.NameComparisonMode = nameComparison;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder Value(this RequiredAttributeDescriptorBuilder builder, string value)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Value = value;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder ValueComparisonMode(
            this RequiredAttributeDescriptorBuilder builder,
            RequiredAttributeDescriptor.ValueComparisonMode valueComparison)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ValueComparisonMode = valueComparison;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder AddDiagnostic(this RequiredAttributeDescriptorBuilder builder, RazorDiagnostic diagnostic)
        {
            builder.Diagnostics.Add(diagnostic);

            return builder;
        }
    }
}
