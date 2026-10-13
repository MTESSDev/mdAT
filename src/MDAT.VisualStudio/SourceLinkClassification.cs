using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MDAT.VisualStudio
{
    /// <summary>Draws the link to the C# test like a link (blue, underlined) so that it is seen as something to click.</summary>
    internal static class SourceLinkClassification
    {
        public const string Name = "mdat.sourcelink";

        [Export(typeof(ClassificationTypeDefinition))]
        [Microsoft.VisualStudio.Utilities.Name(Name)]
        internal static ClassificationTypeDefinition? Definition = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = SourceLinkClassification.Name)]
    [Microsoft.VisualStudio.Utilities.Name(SourceLinkClassification.Name)]
    [UserVisible(true)]
    internal sealed class SourceLinkFormat : ClassificationFormatDefinition
    {
        public SourceLinkFormat()
        {
            DisplayName = "mdAT link to the C# test";
            ForegroundColor = Color.FromRgb(0x3A, 0x96, 0xDD);
            TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ClassificationTag))]
    [ContentType("text")]
    [Microsoft.VisualStudio.Utilities.Name("mdAT source link tagger")]
    internal sealed class SourceLinkTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService Registry { get; set; } = null!;

        public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var type = Registry.GetClassificationType(SourceLinkClassification.Name);

            return buffer.Properties.GetOrCreateSingletonProperty(() => new SourceLinkTagger(buffer, type)) as ITagger<T>;
        }
    }

    internal sealed class SourceLinkTagger : ITagger<ClassificationTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly ClassificationTag _tag;

        public SourceLinkTagger(ITextBuffer buffer, IClassificationType type)
        {
            _buffer = buffer;
            _tag = new ClassificationTag(type);
            _buffer.Changed += OnChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var snapshot = span.Snapshot;
                var first = snapshot.GetLineNumberFromPosition(span.Start);
                var last = snapshot.GetLineNumberFromPosition(span.End);

                for (var number = first; number <= last; number++)
                {
                    var line = snapshot.GetLineFromLineNumber(number);

                    foreach (var link in MdatLinkParser.FindAll(line.GetText()))
                    {
                        yield return new TagSpan<ClassificationTag>(new SnapshotSpan(snapshot, line.Start.Position + link.Start, link.Length), _tag);
                    }
                }
            }
        }

        private void OnChanged(object sender, TextContentChangedEventArgs e)
        {
            // the files are small and the lines of a link are few: the whole text is looked at again
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(e.After, 0, e.After.Length)));
        }
    }
}
