using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VSServerReadyLauncher
{
    internal class ExtensionSettings
    {
        private readonly DateTime _lastWriteTime;
        public IReadOnlyList<ParsedServerReadyAction> ServerReadyActions { get; }
        private bool IsError => ServerReadyActions == null;

        private ExtensionSettings(DateTime lastWriteTime, List<ParsedServerReadyAction> serverReadyActions)
        {
            _lastWriteTime = lastWriteTime;
            this.ServerReadyActions = serverReadyActions;
        }

        private static ExtensionSettings s_instance;
        public static ExtensionSettings Instance
        {
            get => s_instance;
            private set
            {
                if (value == null || value.IsError)
                {
                    Launcher.ClearInstance();
                }

                s_instance = value;
            }
        }

        internal static void OnStartDebugging(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string settingsFilePath = GetSettingsFilePath(serviceProvider);
            if (settingsFilePath == null || !File.Exists(settingsFilePath))
            {
                Instance = null;
                return;
            }

            Json.SettingsFile fileContent = null;
            DateTime lastWriteTime = new DateTime();
            try
            {
                lastWriteTime = File.GetLastWriteTime(settingsFilePath);
                if (Instance?._lastWriteTime == lastWriteTime)
                {
                    // No changes since the last call to OnStartDebugging
                    return;
                }

                string content = File.ReadAllText(settingsFilePath);
                fileContent = JsonConvert.DeserializeObject<Json.SettingsFile>(content);
            }
            catch (Exception e)
            {
                Instance = new ExtensionSettings(lastWriteTime, serverReadyActions: null);
                Utilities.PostError(PackageResources.Err_UnableToParseSettingsFile_Args2.FormatCurrentCulture(settingsFilePath, e.Message));
                return;
            }

            List<ParsedServerReadyAction> serverReadyActions = null;
            try
            {
                var solutionService = serviceProvider.GetRequiredService<IVsSolution>(typeof(IVsSolution));

                serverReadyActions = fileContent.ServerReadyActions
                    .Select(jsonAction => new ParsedServerReadyAction(solutionService, jsonAction, settingsFilePath))
                    .ToList();
            }
            catch (ServerReadyException e)
            {
                Instance = new ExtensionSettings(lastWriteTime, serverReadyActions: null);
                Utilities.PostError(e.Message);
                return;
            }

            Instance = new ExtensionSettings(lastWriteTime, serverReadyActions);
            Launcher.CreateInstance(serviceProvider, Instance);
        }

        public static string GetSettingsFilePath(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionService = serviceProvider.GetRequiredService<IVsSolution>(typeof(IVsSolution));
            object objSolutionFileName;
            if (solutionService.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out objSolutionFileName) == 0)
            {
                string solutionFileName = objSolutionFileName as string;
                if (!string.IsNullOrEmpty(solutionFileName))
                {
                    string settingsFilePath = Path.Combine(Path.GetDirectoryName(solutionFileName), "VSServerReadyLauncher.settings.json");
                    return settingsFilePath;
                }
            }

            return null;
        }
    }
}