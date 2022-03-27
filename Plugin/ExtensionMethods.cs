using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.Plugin
{
    internal static class ExtensionMethods
    {
        public static string? EmptyAsNull(this string str)
        {
            return str == string.Empty ? null : str;
        }

    }
}
