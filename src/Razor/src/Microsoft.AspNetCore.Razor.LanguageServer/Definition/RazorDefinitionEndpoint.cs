﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using SyntaxKind = Microsoft.AspNetCore.Razor.Language.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Definition
{
    internal class RazorDefinitionEndpoint : IDefinitionHandler
    {
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorComponentSearchEngine _componentSearchEngine;
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly ILogger<RazorDefinitionEndpoint> _logger;

        public RazorDefinitionEndpoint(
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            DocumentResolver documentResolver,
            RazorComponentSearchEngine componentSearchEngine,
            RazorDocumentMappingService documentMappingService,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher ?? throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _componentSearchEngine = componentSearchEngine ?? throw new ArgumentNullException(nameof(componentSearchEngine));
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
            _logger = loggerFactory.CreateLogger<RazorDefinitionEndpoint>();
        }

        public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DefinitionRegistrationOptions
            {
                DocumentSelector = RazorDefaults.Selector,
            };
        }

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
        // The return type of the handler should be nullable. O# tracking issue:
        // https://github.com/OmniSharp/csharp-language-server-protocol/issues/644
        public async Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
        {
            _logger.LogInformation("Starting go-to-def endpoint request.");

            if (request is null)
            {
                _logger.LogWarning("Request is null.");
                throw new ArgumentNullException(nameof(request));
            }

            var documentSnapshot = await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(() =>
            {
                var path = request.TextDocument.Uri.GetAbsoluteOrUNCPath();
                _documentResolver.TryResolveDocument(path, out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken).ConfigureAwait(false);

            if (documentSnapshot is null)
            {
                _logger.LogWarning("Document snapshot is null for document.");
                return null;
            }

            if (!FileKinds.IsComponent(documentSnapshot.FileKind))
            {
                _logger.LogInformation($"FileKind '{documentSnapshot.FileKind}' is not a component type.");
                return null;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                _logger.LogInformation("Generated document is unsupported.");
                return null;
            }

            var (originTagDescriptor, attributeDescriptor) = await GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, request.Position, _logger).ConfigureAwait(false);
            if (originTagDescriptor is null)
            {
                _logger.LogInformation("Origin TagHelper descriptor is null.");
                return null;
            }

            var originComponentDocumentSnapshot = await _componentSearchEngine.TryLocateComponentAsync(originTagDescriptor).ConfigureAwait(false);
            if (originComponentDocumentSnapshot is null)
            {
                _logger.LogInformation("Origin TagHelper document snapshot is null.");
                return null;
            }

            _logger.LogInformation($"Definition found at file path: {originComponentDocumentSnapshot.FilePath}");

            var range = await GetNavigateRangeAsync(originComponentDocumentSnapshot, attributeDescriptor, cancellationToken);

            var originComponentUri = new UriBuilder
            {
                Path = originComponentDocumentSnapshot.FilePath,
                Scheme = Uri.UriSchemeFile,
                Host = string.Empty,
            }.Uri;

            return new LocationOrLocationLinks(new[]
            {
                new LocationOrLocationLink(new Location
                {
                    Uri = originComponentUri,
                    Range = range,
                }),
            });
        }

        private async Task<Range> GetNavigateRangeAsync(DocumentSnapshot documentSnapshot, BoundAttributeDescriptor? attributeDescriptor, CancellationToken cancellationToken)
        {
            if (attributeDescriptor is not null)
            {
                _logger.LogInformation("Attempting to get definition from an attribute directly.");

                var originCodeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
                var range = await TryGetPropertyRangeAsync(originCodeDocument, attributeDescriptor.GetPropertyName(), _documentMappingService, _logger, cancellationToken).ConfigureAwait(false);

                if (range is not null)
                {
                    return range;
                }
            }

            // When navigating from a start or end tag, we just take the user to the top of the file.
            // If we were trying to navigate to a property, and we couldn't find it, we can at least take
            // them to the file for the component. If the property was defined in a partial class they can
            // at least then press F7 to go there.
            return new Range(new Position(0, 0), new Position(0, 0));
        }

        internal static async Task<Range?> TryGetPropertyRangeAsync(RazorCodeDocument codeDocument, string propertyName, RazorDocumentMappingService documentMappingService, ILogger logger, CancellationToken cancellationToken)
        {
            // Parse the C# file and find the property that matches the name.
            // We don't worry about parameter attributes here for two main reasons:
            //   1. We don't have symbolic information, so the best we could do would be checking for any
            //      attribute named Parameter, regardless of which namespace. It also means we would have
            //      to do more checks for all of the various ways that the attribute could be specified
            //      (eg fully qualified, aliased, etc.)
            //   2. Since C# doesn't allow multiple properties with the same name, and we're doing a case
            //      sensitive search, we know the property we find is the one the user is trying to encode in a
            //      tag helper attribute. If they don't have the [Parameter] attribute then the Razor compiler
            //      will error, but allowing them to Go To Def on that property regardless, actually helps
            //      them fix the error.
            var csharpText = codeDocument.GetCSharpSourceText();
            var syntaxTree = CSharpSyntaxTree.ParseText(csharpText, cancellationToken: cancellationToken);
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Since we know how the compiler generates the C# source we can be a little specific here, and avoid
            // long tree walks. If the compiler ever changes how they generate their code, the tests for this will break
            // so we'll know about it.
            if (root is CompilationUnitSyntax compilationUnit &&
                compilationUnit.Members[0] is NamespaceDeclarationSyntax namespaceDeclaration &&
                namespaceDeclaration.Members[0] is ClassDeclarationSyntax classDeclaration)
            {
                var property = classDeclaration
                    .Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(p => p.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal))
                    .FirstOrDefault();

                if (property is null)
                {
                    // The property probably exists in a partial class
                    logger.LogInformation("Could not find property in the generated source. Comes from partial?");
                    return null;
                }

                var range = property.Identifier.Span.AsRange(csharpText);
                if (documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, range, out var originalRange))
                {
                    return originalRange;
                }

                logger.LogInformation("Property found but couldn't map its location.");
            }

            logger.LogInformation("Generated C# was not in expected shape (CompilationUnit -> Namespace -> Class)");

            return null;
        }

        internal static async Task<(TagHelperDescriptor?, BoundAttributeDescriptor?)> GetOriginTagHelperBindingAsync(
            DocumentSnapshot documentSnapshot,
            RazorCodeDocument codeDocument,
            Position position,
            ILogger logger)
        {
            var sourceText = await documentSnapshot.GetTextAsync().ConfigureAwait(false);
            var linePosition = new LinePosition(position.Line, position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(hostDocumentIndex, position.Line, position.Character);

            var change = new SourceChange(location.AbsoluteIndex, length: 0, newText: string.Empty);
            var syntaxTree = codeDocument.GetSyntaxTree();
            if (syntaxTree?.Root is null)
            {
                logger.LogInformation("Could not retrieve syntax tree.");
                return (null, null);
            }

            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner is null)
            {
                logger.LogInformation("Could not locate owner.");
                return (null, null);
            }

            var node = owner.Ancestors().FirstOrDefault(n =>
                n.Kind == SyntaxKind.MarkupTagHelperStartTag ||
                n.Kind == SyntaxKind.MarkupTagHelperEndTag);
            if (node is null)
            {
                logger.LogInformation("Could not locate ancestor of type MarkupTagHelperStartTag or MarkupTagHelperEndTag.");
                return (null, null);
            }

            var name = GetStartOrEndTagName(node);
            if (name is null)
            {
                logger.LogInformation("Could not retrieve name of start or end tag.");
                return (null, null);
            }

            string? propertyName = null;

            // If we're on an attribute then just validate against the attribute name
            if (owner.Parent is MarkupTagHelperAttributeSyntax attribute)
            {
                // Normal attribute, ie <Component attribute=value />
                name = attribute.Name;
                propertyName = attribute.TagHelperAttributeInfo.Name;
            }
            else if (owner.Parent is MarkupMinimizedTagHelperAttributeSyntax minimizedAttribute)
            {
                // Minimized attribute, ie <Component attribute />
                name = minimizedAttribute.Name;
                propertyName = minimizedAttribute.TagHelperAttributeInfo.Name;
            }

            if (!name.Span.Contains(location.AbsoluteIndex))
            {
                logger.LogInformation($"Tag name or attributes's span does not contain location's absolute index ({location.AbsoluteIndex}).");
                return (null, null);
            }

            if (node.Parent is not MarkupTagHelperElementSyntax tagHelperElement)
            {
                logger.LogInformation("Parent of start or end tag is not a MarkupTagHelperElement.");
                return (null, null);
            }

            var originTagDescriptor = tagHelperElement.TagHelperInfo.BindingResult.Descriptors.FirstOrDefault(d => !d.IsAttributeDescriptor());
            if (originTagDescriptor is null)
            {
                logger.LogInformation("Origin TagHelper descriptor is null.");
                return (null, null);
            }

            var attributeDescriptor = (propertyName is not null)
                ? originTagDescriptor.BoundAttributes.FirstOrDefault(a => a.Name.Equals(propertyName, StringComparison.Ordinal))
                : null;

            return (originTagDescriptor, attributeDescriptor);
        }

        private static SyntaxNode? GetStartOrEndTagName(SyntaxNode node)
        {
            return node switch
            {
                MarkupTagHelperStartTagSyntax tagHelperStartTag => tagHelperStartTag.Name,
                MarkupTagHelperEndTagSyntax tagHelperEndTag => tagHelperEndTag.Name,
                _ => null
            };
        }
    }
}
