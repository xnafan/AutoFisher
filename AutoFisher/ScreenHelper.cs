using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoFisher
{
    public static class ScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        public static Rectangle GetWindowScreenArea(IntPtr hWnd)
        {
            IntPtr hMonitor = MonitorFromWindow(hWnd, 0);
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            //Console.WriteLine(monitorInfo.cbSize);
            GetMonitorInfo(hMonitor, ref monitorInfo);

            Rectangle windowBounds = GetWindowBounds(hWnd);
            Rectangle screenArea = monitorInfo.rcMonitor.ToRectangle();
           // Console.WriteLine(screenArea);
            if (!screenArea.Contains(windowBounds))
            {
                // Adjust the window bounds to fit within the screen area
                windowBounds.Intersect(screenArea);
            }

            //since the scaling is defined from the primary screen
            //we only scale when we are on another screen.
            //in my case the secondary screen runs 100% scaling. This may affect your results.
            if (screenArea.X != 0)
            {
                //// Adjust for DPI scaling
                float dpiScaleX = GetDpiScaleX(hWnd);
                float dpiScaleY = GetDpiScaleY(hWnd);
                //Console.WriteLine(dpiScaleY);
                windowBounds.X = (int)(windowBounds.X / dpiScaleX);
                windowBounds.Y = (int)(windowBounds.Y / dpiScaleY);
                windowBounds.Width = (int)(windowBounds.Width / dpiScaleX);
                windowBounds.Height = (int)(windowBounds.Height / dpiScaleY); 
            }

            return windowBounds;
        }

        private static Rectangle GetWindowBounds(IntPtr hWnd)
        {
            RECT rect;
            GetWindowRect(hWnd, out rect);
            return rect.ToRectangle();
        }

        private static Rectangle ToRectangle(this RECT rect)
        {
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        private static RECT ToRECT(this Rectangle rectangle)
        {
            return new RECT { left = rectangle.Left, top = rectangle.Top, right = rectangle.Right, bottom = rectangle.Bottom };
        }

        private static float GetDpiScaleX(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                IntPtr desktop = graphics.GetHdc();
                int dpiX = GetDeviceCaps(desktop, LOGPIXELSX);
                graphics.ReleaseHdc(desktop);
                return dpiX / 96f;
            }
        }

        private static float GetDpiScaleY(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                IntPtr desktop = graphics.GetHdc();
                int dpiY = GetDeviceCaps(desktop, LOGPIXELSY);
                graphics.ReleaseHdc(desktop);
                return dpiY / 96f;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    }

}
