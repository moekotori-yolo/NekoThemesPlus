using System;
using System.Diagnostics;

namespace NekoThemesPlus.Native
{
    public static class WindowHandleProvider
    {
        public static bool TryGetMainWindowHandle(out IntPtr handle)
        {
            handle = IntPtr.Zero;
#if UNITY_EDITOR_WIN
            Process process = Process.GetCurrentProcess();
            process.Refresh();
            handle = process.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                return true;
            }

            uint expectedProcessId = (uint)process.Id;
            IntPtr found = IntPtr.Zero;
            WindowsNative.EnumWindows(delegate(IntPtr candidate, IntPtr parameter)
            {
                uint processId;
                WindowsNative.GetWindowThreadProcessId(candidate, out processId);
                if (processId == expectedProcessId && WindowsNative.IsWindowVisible(candidate))
                {
                    found = candidate;
                    return false;
                }

                return true;
            }, IntPtr.Zero);
            handle = found;
#endif
            return handle != IntPtr.Zero;
        }
    }
}
