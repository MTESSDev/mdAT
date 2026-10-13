using System;
using IServiceProvider = System.IServiceProvider;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MDAT.VisualStudio
{
    /// <summary>
    /// F7 (View Code) goes from a markdown test file to the C# test, and from a test method to its markdown file.
    /// Anywhere else it does what it always did.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class ViewCodeCommandListener : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService Adapters { get; set; } = null!;

        [Import]
        internal ITextDocumentFactoryService Documents { get; set; } = null!;

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; } = null!;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var view = Adapters.GetWpfTextView(textViewAdapter);

            if (view is null || !Documents.TryGetTextDocument(view.TextBuffer, out var document))
            {
                return;
            }

            var extension = Path.GetExtension(document.FilePath ?? "");

            if (!ViewCodeCommandFilter.Handles(extension))
            {
                return;
            }

            var filter = new ViewCodeCommandFilter(view, document, ServiceProvider);

            if (textViewAdapter.AddCommandFilter(filter, out var next) == VSConstants.S_OK)
            {
                filter.Next = next;
            }
        }
    }

    internal sealed class ViewCodeCommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _view;
        private readonly ITextDocument _document;
        private readonly IServiceProvider _services;

        public ViewCodeCommandFilter(IWpfTextView view, ITextDocument document, IServiceProvider services)
        {
            _view = view;
            _document = document;
            _services = services;
        }

        public IOleCommandTarget? Next { get; set; }

        public static bool Handles(string extension) =>
            extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

        private static bool IsViewCode(Guid group, uint id) =>
            group == VSConstants.GUID_VSStandardCommandSet97 && id == (uint)VSConstants.VSStd97CmdID.ViewCode;

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // the answer is "yes" without looking: this is asked all the time, reading the file is left to the command itself
            if (cCmds == 1 && IsViewCode(pguidCmdGroup, prgCmds[0].cmdID))
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return VSConstants.S_OK;
            }

            return Next is null ? (int)Constants.OLECMDERR_E_NOTSUPPORTED : Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (IsViewCode(pguidCmdGroup, nCmdID))
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    GoToTheOtherFile();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
                {
                    FileOpener.Notify(_services, $"mdAT: {ex.Message}");
                }

                return VSConstants.S_OK;
            }

            return Next is null ? (int)Constants.OLECMDERR_E_NOTSUPPORTED : Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void GoToTheOtherFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var path = _document.FilePath;
            var snapshot = _view.TextSnapshot;
            var caret = _view.Caret.Position.BufferPosition;

            if (Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                OpenMarkdownOfMethod(path, snapshot.GetText(), caret.Position);
            }
            else
            {
                OpenTestOfMarkdown(path, snapshot, caret);
            }
        }

        private void OpenTestOfMarkdown(string path, ITextSnapshot snapshot, SnapshotPoint caret)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // the link of the line of the caret, else the first one of the file
            MdatLink? chosen = null;
            var caretLine = caret.GetContainingLine().LineNumber;

            foreach (var line in snapshot.Lines)
            {
                var link = MdatLinkParser.FindAll(line.GetText()).Cast<MdatLink?>().FirstOrDefault();

                if (link is null)
                {
                    continue;
                }

                if (line.LineNumber == caretLine)
                {
                    chosen = link;
                    break;
                }

                chosen ??= link;
            }

            if (chosen is null)
            {
                FileOpener.Notify(_services, "mdAT: this markdown file has no link to a C# test.");
                return;
            }

            FileOpener.Open(_services, MdatLinkParser.Resolve(path, chosen.Value), chosen.Value.Line);
        }

        private void OpenMarkdownOfMethod(string path, string text, int caret)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var spot = MethodLocator.Locate(text, caret);

            if (spot is null)
            {
                FileOpener.Notify(_services, "mdAT: the cursor is not in a method.");
                return;
            }

            var markdown = MarkdownFinder.Find(path, spot.Value.ClassName, spot.Value.MethodName);

            if (markdown is null)
            {
                FileOpener.Notify(_services, $"mdAT: no markdown file for {spot.Value.ClassName}.{spot.Value.MethodName} (the file gets its link when the tests are discovered).");
                return;
            }

            FileOpener.Open(_services, markdown.Value.Path, markdown.Value.Line);
        }
    }
}
