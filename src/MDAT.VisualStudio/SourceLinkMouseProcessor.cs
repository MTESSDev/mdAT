using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MDAT.VisualStudio
{
    [Export(typeof(IMouseProcessorProvider))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Name("mdAT source link mouse")]
    internal sealed class SourceLinkMouseProcessorProvider : IMouseProcessorProvider
    {
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; } = null!;

        [Import]
        internal ITextDocumentFactoryService Documents { get; set; } = null!;

        public IMouseProcessor? GetAssociatedProcessor(IWpfTextView view)
        {
            return Documents.TryGetTextDocument(view.TextBuffer, out var document)
                ? new SourceLinkMouseProcessor(view, document, ServiceProvider)
                : null;
        }
    }

    /// <summary>Ctrl + click on the link to the C# test of a markdown file opens the C# file on the line of the test.</summary>
    internal sealed class SourceLinkMouseProcessor : MouseProcessorBase
    {
        private readonly IWpfTextView _view;
        private readonly ITextDocument _document;
        private readonly IServiceProvider _services;
        private bool _handCursor;

        public SourceLinkMouseProcessor(IWpfTextView view, ITextDocument document, IServiceProvider services)
        {
            _view = view;
            _document = document;
            _services = services;
        }

        public override void PreprocessMouseMove(MouseEventArgs e)
        {
            var overLink = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && FindLink(e, out _);

            if (overLink && !_handCursor)
            {
                _view.VisualElement.Cursor = Cursors.Hand;
                _handCursor = true;
            }
            else if (!overLink && _handCursor)
            {
                _view.VisualElement.ClearValue(FrameworkElement.CursorProperty);
                _handCursor = false;
            }
        }

        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || !FindLink(e, out var link))
            {
                return;
            }

            e.Handled = true;

            try
            {
                Open(MdatLinkParser.Resolve(_document.FilePath, link), link.Line);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Notify($"mdAT: {ex.Message}");
            }
        }

        private bool FindLink(MouseEventArgs e, out MdatLink link)
        {
            link = default;

            var point = e.GetPosition(_view.VisualElement);
            var viewLine = _view.TextViewLines?.GetTextViewLineContainingYCoordinate(point.Y + _view.ViewportTop);

            if (viewLine is null)
            {
                return false;
            }

            var position = viewLine.GetBufferPositionFromXCoordinate(point.X + _view.ViewportLeft);

            if (position is null)
            {
                return false;
            }

            var line = position.Value.GetContainingLine();

            return MdatLinkParser.TryFind(line.GetText(), position.Value.Position - line.Start.Position, out link);
        }

        private void Open(string path, int line)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!File.Exists(path))
            {
                Notify($"mdAT: the file {path} does not exist.");
                return;
            }

            VsShellUtilities.OpenDocument(_services, path, Guid.Empty, out _, out _, out var frame);

            var textView = frame is null ? null : VsShellUtilities.GetTextView(frame);

            if (textView is not null && line > 0)
            {
                textView.SetCaretPos(line - 1, 0);
                textView.CenterLines(line - 1, 1);
            }
        }

        private void Notify(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_services.GetService(typeof(SVsStatusbar)) is IVsStatusbar statusBar)
            {
                statusBar.SetText(message);
            }
        }
    }
}
