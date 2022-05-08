using System;
using System.Runtime.InteropServices;

namespace SpiderStud.Helpers
{
    internal static class SpiderStudRuntime
    {
        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static bool IsRunningOnWindows()
        {
#if NET45 || NET40
            return true;
#else
            return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
#endif
        }
    }
}