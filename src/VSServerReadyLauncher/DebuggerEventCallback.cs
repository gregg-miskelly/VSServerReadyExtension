using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace VSServerReadyLauncher
{
    class DebuggerEventCallback : IDebugEventCallback2, IVsDebuggerEvents
    {
        static public DebuggerEventCallback Instance => s_instance;
        static DebuggerEventCallback s_instance;
        readonly IServiceProvider _serviceProvider;
        readonly IVsDebugger _debugger;
        DBGMODE _currentMode;
        uint _cookie;
        Guid IID_IDebugOutputStringEvent2 = typeof(IDebugOutputStringEvent2).GUID;

        private DebuggerEventCallback(IServiceProvider serviceProvider, IVsDebugger debugger, DBGMODE currentMode)
        {
            _serviceProvider = serviceProvider;
            _debugger = debugger;
            _currentMode = currentMode;
        }

        public static void EnsureRegistered(IServiceProvider serviceProvider)
        {
            int hr;

            ThreadHelper.ThrowIfNotOnUIThread();

            if (Instance == null)
            {
                IVsDebugger debuggerPackage = serviceProvider.GetRequiredService<IVsDebugger>(typeof(IVsDebugger));
                if (debuggerPackage == null)
                {
                    throw new InvalidOperationException();
                }

                DBGMODE[] modeArray = new DBGMODE[1];
                hr = debuggerPackage.GetMode(modeArray);
                ErrorHandler.ThrowOnFailure(hr);

                var instance = new DebuggerEventCallback(serviceProvider, debuggerPackage, modeArray[0]);
                debuggerPackage.AdviseDebugEventCallback(instance);
                debuggerPackage.AdviseDebuggerEvents(instance, out instance._cookie);
                s_instance = instance;
            }
        }

        public static void EnsureUnregistered()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var instance = Interlocked.Exchange<DebuggerEventCallback>(ref s_instance, null);
            if (instance != null)
            {
                instance._debugger.UnadviseDebugEventCallback(instance);
                instance._debugger.UnadviseDebuggerEvents(instance._cookie);
            }
        }

        public bool IsDebugging => IsDebugMode(_currentMode);

        int IVsDebuggerEvents.OnModeChange(DBGMODE newMode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsDebugMode(newMode) && !IsDebugMode(_currentMode))
            {
                ExtensionSettings.OnStartDebugging(_serviceProvider);
            }
            else if (!IsDebugMode(newMode) && IsDebugMode(_currentMode))
            {
                Launcher.Instance?.OnStopDebugging();
            }

            _currentMode = newMode;
            return 0;
        }

        private bool IsDebugMode(DBGMODE mode)
        {
            switch (mode)
            {
                case DBGMODE.DBGMODE_Design:
                    return false;

                case DBGMODE.DBGMODE_Break:
                case DBGMODE.DBGMODE_Run:
                    return true;

                default:
                    Debug.Fail("Unexpected debug mode");
                    throw new ArgumentOutOfRangeException("newMode");
            }
        }

        int IDebugEventCallback2.Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 eventObj, ref Guid iidEvent, uint attrib)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (iidEvent == IID_IDebugOutputStringEvent2)
                {
                    HandleOutputEvent((IDebugOutputStringEvent2)eventObj);
                }

                return 0;
            }
            finally
            {
                ComRelease(engine);
                ComRelease(process);
                ComRelease(program);
                ComRelease(thread);
                ComRelease(eventObj);
            }
        }

        private void HandleOutputEvent(IDebugOutputStringEvent2 eventObj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (eventObj.GetString(out string outputText) != VSConstants.S_OK || string.IsNullOrEmpty(outputText))
            {
                return;
            }

            Launcher.Instance?.OnOutputString(outputText);
        }

        private void ComRelease(object o)
        {
            if (o != null && System.Runtime.InteropServices.Marshal.IsComObject(o))
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(o);
            }
        }

    }
}
