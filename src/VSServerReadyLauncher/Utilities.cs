using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;

namespace VSServerReadyLauncher
{
    public static class Utilities
    {
        public static void PostError(string message)
        {
            if (ThreadHelper.CheckAccess())
            {
                Action action = () => ShowError(message);
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs -- we are already on the main thread and we want to post to avoid blocking the rest of the current operation
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(action, DispatcherPriority.Background);
#pragma warning restore VSTHRD001
            }
            else
            {
                async Task PostAsync()
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ShowError(message);
                }

                _ = PostAsync();
            }
        }

        public static void ShowError(string message)
        {
            const int E_VSSERVERREADYLAUNCHER_ERROR = unchecked((int)0x84D40001);

            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = (IVsUIShell)Package.GetGlobalService(typeof(IVsUIShell));

            shell.SetErrorInfo(E_VSSERVERREADYLAUNCHER_ERROR, message, 0, null, nameof(VSServerReadyLauncher));
            shell.ReportErrorInfo(E_VSSERVERREADYLAUNCHER_ERROR);
        }
    }
}
