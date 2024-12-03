using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

        using System;
using System.Runtime.InteropServices;
namespace AutoFisher
{
    internal class StaticRightClicker
    {

        // Import the necessary Windows API functions
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Constants for mouse events
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public static void RightClickWindow(string windowTitle)
        {
            // Find the window you want to send the right-click to
            IntPtr hWnd = FindWindow(null, windowTitle); // Replace "Window Title" with the actual window title

            if (hWnd != IntPtr.Zero)
            {
                // Set the window as the foreground window
                SetForegroundWindow(hWnd);
                Console.WriteLine("Found minecraftwindow");
                // Simulate the right-click using mouse events
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
            else
            {
                Console.WriteLine("Window not found.");
            }
        }
    }
}
