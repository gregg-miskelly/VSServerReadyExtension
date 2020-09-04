using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VSServerReadyLauncher.Json;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace VSServerReadyLauncher
{
    class ParsedServerReadyAction
    {
        public Regex OutputPattern { get; }
        public string ProjectName { get; }
        public IVsHierarchy ProjectHierarchy { get; }
        public bool IsLaunchAttempted { get; set; }

        public ParsedServerReadyAction(IVsSolution solutionService, ServerReadyAction jsonAction, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                this.OutputPattern = new Regex(jsonAction.OutputPattern, RegexOptions.None);
            }
            catch (Exception e)
            {
                throw new ServerReadyException(PackageResources.Err_UnableToParsePattern_Args3.FormatCurrentCulture(filePath, jsonAction.OutputPattern, e.Message));
            }

            IVsHierarchy hierarchy;
            solutionService.GetProjectOfUniqueName(jsonAction.ProjectToLaunch, out hierarchy);
            if (hierarchy == null)
            {
                throw new ServerReadyException(PackageResources.Err_UnableToFindProject_Args2.FormatCurrentCulture(filePath, jsonAction.ProjectToLaunch));
            }

            this.ProjectName = jsonAction.ProjectToLaunch;
            this.ProjectHierarchy = hierarchy;
        }
    }
}
