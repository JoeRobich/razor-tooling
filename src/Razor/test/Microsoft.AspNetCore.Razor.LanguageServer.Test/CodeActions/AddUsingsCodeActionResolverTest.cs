﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class AddUsingsCodeActionResolverTest : LanguageServerTestBase
    {
        private readonly DocumentResolver _emptyDocumentResolver = Mock.Of<DocumentResolver>(r => r.TryResolveDocument(It.IsAny<string>(), out It.Ref<DocumentSnapshot>.IsAny) == false, MockBehavior.Strict);

        [Fact]
        public async Task Handle_MissingFile()
        {
            // Arrange
            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, _emptyDocumentResolver);
            var data = JObject.FromObject(new AddUsingsCodeActionParams()
            {
                Uri = new Uri("c:/Test.razor"),
                Namespace = "System"
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_Unsupported()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"";
            var codeDocument = CreateCodeDocument(contents);
            codeDocument.SetUnsupported();

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var data = JObject.FromObject(new AddUsingsCodeActionParams()
            {
                Uri = new Uri(documentPath),
                Namespace = "System"
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_AddOneUsingToEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = string.Empty;
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(0, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToComponentPageDirective()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/\"{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToPageDirective()
        {
            // Arrange
            var documentPath = "c:/Test.cshtml";
            var documentUri = new Uri(documentPath);
            var contents = $"@page{Environment.NewLine}@model IndexModel";

            var projectItem = new TestRazorProjectItem("c:/Test.cshtml", "c:/Test.cshtml", "Test.cshtml") { Content = contents };
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, (builder) =>
            {
                PageDirective.Register(builder);
                ModelDirective.Register(builder);
            });
            var codeDocument = projectEngine.Process(projectItem);
            codeDocument.SetFileKind(FileKinds.Legacy);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToHTML()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"<table>{Environment.NewLine}<tr>{Environment.NewLine}</tr>{Environment.NewLine}</table>";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(0, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToNamespace()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@namespace Testing{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToPageAndNamespace()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/\"{Environment.NewLine}@namespace Testing{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(2, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToUsings()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using System";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "System.Linq"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System.Linq{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneNonSystemUsingToSystemUsings()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using System{Environment.NewLine}@using System.Linq{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(LegacyDispatcher, CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespace = "Microsoft.AspNetCore.Razor.Language"
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.Equal(2, firstEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Language{Environment.NewLine}", firstEdit.NewText);
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var sourceTextChars = new char[codeDocument.Source.Length];
            codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
            var sourceText = SourceText.From(new string(sourceTextChars));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(sourceText), MockBehavior.Strict);
            var documentResolver = new Mock<DocumentResolver>(MockBehavior.Strict);
            documentResolver
                .Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }

        private static RazorCodeDocument CreateCodeDocument(string text)
        {
            var fileName = "Test.razor";
            var filePath = $"c:/{fileName}";
            var projectItem = new TestRazorProjectItem(filePath, filePath, fileName) { Content = text };
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, (builder) => PageDirective.Register(builder));
            var codeDocument = projectEngine.Process(projectItem);
            codeDocument.SetFileKind(FileKinds.Component);
            return codeDocument;
        }
    }
}
