using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSServerReadyLauncher
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenSettingsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b6d8a69c-8c10-4eee-9016-dca66822196a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSettingsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenSettingsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this._package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuCommand = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenSettingsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in OpenExtensionSettings's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenSettingsCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string settingsFile = ExtensionSettings.GetSettingsFilePath(_package);
            if (settingsFile == null)
            {
                throw new InvalidOperationException();
            }

            if (!File.Exists(settingsFile))
            {
                using (Stream sourceStream = typeof(OpenSettingsCommand).Assembly.GetManifestResourceStream("VSServerReadyLauncher.settings.json"))
                {
                    int length = (int)sourceStream.Length;
                    var fileBytes = new byte[length];
                    sourceStream.Read(fileBytes, 0, length);
                    File.WriteAllBytes(settingsFile, fileBytes);
                }
            }

            var dte = _package.GetRequiredService<EnvDTE.DTE>(typeof(EnvDTE.DTE));
            dte.ItemOperations.OpenFile(settingsFile);
        }
    }
}
