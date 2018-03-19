using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCover.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MiniCover.Instrumentation
{
    internal static class MethodDefinitionExtensions
    {
        public static Document GetDocument(this MethodDefinition methodDefinition)
        {
            return methodDefinition.DebugInformation.SequencePoints.Select(a => a.Document).First();
        }
    }
    internal class SourceFile
    {
        private readonly Document document;
        private readonly ICollection<MethodInstrumenter> methods = new List<MethodInstrumenter>();
        
        internal SourceFile(Document document)
        {
            this.document = document;
        }

        public string SourcePath => document.Url;

        public bool IsDocumentContainBy(IEnumerable<string> sourceFilesToInstrument)
        {
            return sourceFilesToInstrument.Contains(this.document.Url);
        }

        public bool HasSourceFileChanged()
        {
            return this.document.FileHasChanged();
        }

        private void AddMethod(MethodDefinition methodDefinition)
        {
            var instrumenter = new MethodInstrumenter(methodDefinition, this);
            this.methods.Add(instrumenter);
        }

        public string GetSourceRelativePathFrom(string workDirectoryPath)
        {
            Uri file = new Uri(document.Url);
            Uri folder = new Uri(workDirectoryPath);
            string relativePath =
                Uri.UnescapeDataString(
                    folder.MakeRelativeUri(file)
                        .ToString()
                        .Replace('/', Path.DirectorySeparatorChar)
                );
            return relativePath;
        }

        public static IEnumerable<SourceFile> FromAssembly(AssemblyDefinition definition)
        {
            var sources = new Dictionary<string, SourceFile>();
            foreach (var type in  definition.MainModule.GetTypes())
            {
                foreach (var method in type.GetAllMethods())
                {
                    var document = method.GetDocument();
                    if (!sources.TryGetValue(document.Url, out var sourceFile))
                    {
                        sourceFile = new SourceFile(document);
                        sources.Add(document.Url, sourceFile);
                    }

                    sourceFile.AddMethod(method);
                }
            }

            return sources.Values;
        }

        public void Instrument(string normalizedWorkDir, Func<string, IEnumerable<string>> readLines)
        {
            var sourceRelativePath = GetSourceRelativePathFrom(normalizedWorkDir);
            var fileLines = readLines(this.SourcePath);
            foreach (var method in methods)
            {
                method.Instrument(sourceRelativePath);
            }
        }
    }

    internal class MethodInstrumenter
    {
        private readonly MethodDefinition methodDefinition;
        private readonly SourceFile sourceFile;

        public MethodInstrumenter(MethodDefinition methodDefinition, SourceFile sourceFile)
        {
            this.methodDefinition = methodDefinition;
            this.sourceFile = sourceFile;
        }

        public void Instrument(string sourceRelativePath)
        {
            
        }
    }
}