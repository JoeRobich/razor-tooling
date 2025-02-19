﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Razor.IntegrationTests
{
    public class ProjectTests : AbstractRazorEditorTest
    {
        [IdeFact]
        public async Task CreateFromTemplateAsync()
        {
            await TestServices.SolutionExplorer.OpenFileAsync(BlazorProjectName, CounterRazorFile, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
        }
    }
}
