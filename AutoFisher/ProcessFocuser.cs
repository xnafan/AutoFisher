using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoFisher
{
    public static class ProcessFocuser
    {

        #region Enums and System32 calls
        [DllImport("user32.dll")]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        private const int ALT = 0xA4;
        private const int EXTENDEDKEY = 0x1;
        private const int KEYUP = 0x2;


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); 
        #endregion

        public static void SetFocusOnProcessIfExists(string searchString)
        {

            var ps = Process.GetProcesses().Where(p => p.MainWindowTitle.ToLower().Contains(searchString.ToLower()));
            var process = Process.GetProcesses().FirstOrDefault(p => p.MainWindowTitle.ToLower().Contains(searchString.ToLower()));
            if (process != null) { ActivateWindow(process.MainWindowHandle); }

            //// check if the window is hidden / minimized
            //if (process.MainWindowHandle == IntPtr.Zero)
            //{
            //    // the window is hidden so try to restore it before setting focus.
            //    ShowWindow(process.Handle, ShowWindowEnum.Restore);
            //}
            //Thread.Sleep(750);
            //// set user the focus to the window
            //SetForegroundWindow(process.MainWindowHandle);

        }


        


        public static void ActivateWindow(IntPtr mainWindowHandle)
        {
            // Guard: check if window already has focus.
            if (mainWindowHandle == GetForegroundWindow()) return;

            // Show window maximized.
            ShowWindow(mainWindowHandle, ShowWindowEnum.ShowMaximized);

            // Simulate an "ALT" key press.
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);

            // Simulate an "ALT" key release.
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);

            Thread.Sleep(250);

            // Show window in forground.
            SetForegroundWindow(mainWindowHandle);
        }
    }
}