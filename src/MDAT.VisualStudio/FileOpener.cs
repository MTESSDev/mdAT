using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDAT.VisualStudio
{
    /// <summary>Opens a file in Visual Studio on a line, and tells what went wrong in the status bar.</summary>
    internal static class FileOpener
    {
        public static void Open(IServiceProvider services, string path, int line)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!File.Exists(path))
            {
                Notify(services, $"mdAT: the file {path} does not exist.");
                return;
            }

            VsShellUtilities.OpenDocument(services, path, Guid.Empty, out _, out _, out var frame);

            var textView = frame is null ? null : VsShellUtilities.GetTextView(frame);

            if (textView is not null && line > 0)
            {
                textView.SetCaretPos(line - 1, 0);
                textView.CenterLines(line - 1, 1);
            }
        }

        public static void Notify(IServiceProvider services, string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (services.GetService(typeof(SVsStatusbar)) is IVsStatusbar statusBar)
            {
                statusBar.SetText(message);
            }
        }
    }
}
