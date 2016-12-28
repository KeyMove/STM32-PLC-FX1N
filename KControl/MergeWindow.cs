using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeyMove.Tools
{
    static class MergeWindow
    {
        //static Thread GetApp;
        static IntPtr DesHandle;
        static IntPtr SrcHandle;
        static Process SrcApp;

        [DllImport("user32.dll")]
        private static extern int SetParent(IntPtr hWndChild, IntPtr hWndParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter,
                    int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint newLong);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, short State);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const int HWND_TOP = 0x0;

        private const int WM_COMMAND = 0x0112;
        private const int WM_QT_PAINT = 0xC2DC;
        private const int WM_PAINT = 0x000F;
        private const int WM_SIZE = 0x0005;

        private const int SWP_FRAMECHANGED = 0x0020;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;

        const int GWL_STYLE = -16;

        const uint WS_CAPTION = 0x00C00000;
        const uint WS_THICKFRAME = 0x00040000;
        const uint WS_POPUP = 0x80000000;
        const uint WS_VISIBLE = 0x10000000;



        static public bool Merge(IntPtr Des,Process Src)
        {
            DesHandle = Des;
            SrcApp = Src;
            new Thread(AppMerge).Start();
            return true;
        }

        static public bool MergeWindowName(IntPtr Des,string WindowName,bool wait=false)
        {
            DesHandle = Des;
            IntPtr Src = FindWindow(null, WindowName);
            if (Src == IntPtr.Zero)
                return false;
            SrcHandle = Src;
            if(!wait)
                new Thread(WindowMerge).Start();
            else
            {
                WindowMerge();
            }
            return true;
        }

        static public bool MergeApp(IntPtr Des,string FileName,string arg="")
        {
            DesHandle = Des;
            SrcApp = new Process();
            SrcApp.StartInfo.FileName = FileName;
            SrcApp.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            SrcApp.StartInfo.Arguments = arg;
            new Thread(AppMerge).Start();
            return true;
        }

        static void AppMerge()
        {
            try
            {
                SrcApp.Start();
                SrcApp.WaitForInputIdle();
                while (SrcApp.MainWindowHandle == IntPtr.Zero) ;
                SrcHandle = SrcApp.MainWindowHandle;
            }
            catch
            {
                return;
            }
            WindowMerge();
        }

        static void WindowMerge()
        {
            ShowWindow(SrcHandle, 0);
            uint v = GetWindowLong(SrcHandle, GWL_STYLE);
            v &= ~(  WS_THICKFRAME);
            SetWindowLong(SrcHandle, GWL_STYLE, v | WS_POPUP | WS_VISIBLE);
            SetParent(SrcHandle, DesHandle);
            SendMessage(SrcHandle, WM_COMMAND, WM_PAINT, 0);
            PostMessage(SrcHandle, WM_QT_PAINT, 0, 0);
            SetWindowPos(SrcHandle, HWND_TOP, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOSIZE);
            SendMessage(SrcHandle, WM_COMMAND, WM_SIZE, 0);
        }

        public static void SetWindowSize(IntPtr Handle, int w, int h)
        {
            if (Handle == (IntPtr)null) Handle = SrcHandle;
            SetWindowPos(Handle, 0, 0, 0, w, h, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOZORDER);
        }

        public static void SetWindowPos(IntPtr Handle, int x, int y)
        {
            if (Handle == (IntPtr)null) Handle = SrcHandle;
            SetWindowPos(Handle, 0, x, y, 0, 0, SWP_FRAMECHANGED | SWP_NOSIZE | SWP_NOZORDER);
        }
    }
}
