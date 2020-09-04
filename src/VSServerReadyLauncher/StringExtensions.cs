using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSServerReadyLauncher
{
    static class StringExtensions
    {
        public static string FormatCurrentCulture(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
    }
}
