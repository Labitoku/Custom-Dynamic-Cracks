#if UNITY_EDITOR_WIN
using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace CleanEditorLayout
{
    internal static class Win32
    {
        public const int GW_OWNER = 4;
        public const int GW_HWNDNEXT = 2;
        public const int GWL_STYLE = -16;
        public const int SW_NORMAL = 1;
        public const int SW_MAXIMIZE = 3;
        public const int SW_MINIMIZE = 6;
        public const int SM_CYCAPTION = 4;
        public const int MDT_EFFECTIVE_DPI = 0;
        public const int MONITOR_DEFAULTTOPRIMARY = 1;
        public const int MONITOR_DEFAULTTONEAREST = 2;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint WS_CAPTION = 0x00C00000;
        public const uint MF_BYPOSITION = 0x00000400;
        public static IntPtr HWND_TOP = new IntPtr(0);
        public delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);
        [DllImport("user32.dll")] public static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr handle);
        [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr hMenu);
        [DllImport("user32.dll")] public static extern IntPtr GetTopWindow(IntPtr handle);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr handle, uint uCmd);
        [DllImport("user32.dll")] public static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);
        [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] public static extern IntPtr GetWindowText(IntPtr handle, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] public static extern IntPtr GetClassName(IntPtr handle, StringBuilder lpClassName, int nMaxCount);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);
        [DllImport("user32.dll")] public static extern int GetMenuString(IntPtr hMenu, int uItem, StringBuilder lpString, int nMaxCount, uint uFlag);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

        
        public static Vector2Int GetMonitorSize(IntPtr monitorHandle) {
            if(monitorHandle == IntPtr.Zero) return Vector2Int.zero;
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if(GetMonitorInfo(monitorHandle, ref monitorInfo)) {
                int width = monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left;
                int height = monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top;
                return new Vector2Int(width, height);
            }
            return Vector2Int.zero;
        }
        public static Vector2Int GetMonitorPosition(IntPtr monitorHandle) {
            if(monitorHandle == IntPtr.Zero) return Vector2Int.zero;
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if(GetMonitorInfo(monitorHandle, ref monitorInfo)) {
                int posX = monitorInfo.rcMonitor.Left;
                int posY = monitorInfo.rcMonitor.Top;
                return new Vector2Int(posX, posY);
            }
            return Vector2Int.zero;
        }
    }
}
#endif