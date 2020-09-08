using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace VSServerReadyLauncher
{
    internal class Launcher
    {
        public static Launcher Instance { get; private set; }
        private readonly IServiceProvider _serviceProvider;
        private readonly ExtensionSettings _settings;

        private Launcher(IServiceProvider serviceProvider, ExtensionSettings settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        internal static void CreateInstance(IServiceProvider serviceProvider, ExtensionSettings launchSettings)
        {
            if (launchSettings.ServerReadyActions.Count == 0)
            {
                ClearInstance();
            }
            else
            {
                Instance = new Launcher(serviceProvider, launchSettings);
            }
        }

        internal static void ClearInstance()
        {
            Instance = null;
        }

        internal void OnOutputString(string outputText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ParsedServerReadyAction action in _settings.ServerReadyActions)
            {
                if (!action.IsLaunchAttempted && action.OutputPattern.IsMatch(outputText))
                {
                    action.IsLaunchAttempted = true;

                    try
                    {
                        Fire(action);
                    }
                    catch (ServerReadyException e)
                    {
                        Utilities.ShowError(e.Message);
                    }
                }
            }
        }

        internal void OnStopDebugging()
        {
            foreach (ParsedServerReadyAction action in _settings.ServerReadyActions)
            {
                action.IsLaunchAttempted = false;
            }
        }

        private void Fire(ParsedServerReadyAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionBuildManager = _serviceProvider.GetRequiredService<IVsSolutionBuildManager>(typeof(SVsSolutionBuildManager));
            IVsProjectCfg[] vsProjectCfgs = new IVsProjectCfg[1];
            solutionBuildManager.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, action.ProjectHierarchy, vsProjectCfgs);

            IVsDebuggableProjectCfg debuggableCfg = null;

            IVsProjectCfg projectCfg = vsProjectCfgs[0];
            if (projectCfg is IVsProjectCfg2 projectCfg2)
            {
                Guid IID_IVsDebuggableProjectCfg = typeof(IVsDebuggableProjectCfg).GUID;
                IntPtr pDebuggableCfg;
                if (projectCfg2.get_CfgType(ref IID_IVsDebuggableProjectCfg, out pDebuggableCfg) == VSConstants.S_OK && pDebuggableCfg != IntPtr.Zero)
                {
                    debuggableCfg = Marshal.GetObjectForIUnknown(pDebuggableCfg) as IVsDebuggableProjectCfg;
                    Marshal.Release(pDebuggableCfg);
                }
            }
            else
            {
                debuggableCfg = projectCfg as IVsDebuggableProjectCfg;
            }

            if (debuggableCfg == null)
            {
                throw new ServerReadyException(PackageResources.Err_ProjectNotDebuggable_Args1.FormatCurrentCulture(action.ProjectName));
            }

            int hr = debuggableCfg.DebugLaunch(0);
            if (hr < 0)
            {
                Exception e = Marshal.GetExceptionForHR(hr);
                throw new ServerReadyException(PackageResources.Err_ProjectDebugFailed_Args2.FormatCurrentCulture(action.ProjectName, e.Message));
            }
        }
    }
}