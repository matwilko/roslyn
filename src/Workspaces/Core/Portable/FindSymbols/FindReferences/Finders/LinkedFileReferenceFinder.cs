﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class LinkedFileReferenceFinder : IReferenceFinder
    {
        public async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var linkedSymbols = new HashSet<ISymbol>();

            foreach (var location in symbol.DeclaringSyntaxReferences)
            {
                var originalDocument = solution.GetDocument(location.SyntaxTree);

                foreach (var linkedDocumentId in originalDocument.GetLinkedDocumentIds())
                {
                    var linkedDocument = solution.GetDocument(linkedDocumentId);
                    var linkedSyntaxRoot = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    // Defend against constructed solutions with inconsistent linked documents
                    if (!linkedSyntaxRoot.FullSpan.Contains(location.Span))
                    {
                        continue;
                    }

                    var linkedNode = linkedSyntaxRoot.FindNode(location.Span, getInnermostNodeForTie: true);

                    var semanticModel = await linkedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var linkedSymbol = semanticModel.GetDeclaredSymbol(linkedNode, cancellationToken);

                    if (linkedSymbol != null &&
                        linkedSymbol.Kind == symbol.Kind &&
                        linkedSymbol.Name == symbol.Name &&
                        !linkedSymbols.Contains(linkedSymbol))
                    {
                        linkedSymbols.Add(linkedSymbol);
                    }
                }
            }

            return linkedSymbols;
        }

        public Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.EmptyEnumerable<Document>();
        }

        public Task<IEnumerable<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.EmptyEnumerable<Project>();
        }

        public Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(ISymbol symbol, Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.EmptyEnumerable<ReferenceLocation>();
        }
    }
}
