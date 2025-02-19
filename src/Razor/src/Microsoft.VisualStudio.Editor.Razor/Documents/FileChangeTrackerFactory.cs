﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal abstract class FileChangeTrackerFactory : IWorkspaceService
    {
        public abstract FileChangeTracker Create(string filePath);
    }
}
