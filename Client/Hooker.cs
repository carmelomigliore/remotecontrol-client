using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Client
{
    class Hooker
    {
        public Server LeftServer { get; set; }

        public Server RightServer { get; set; }

        private Server _currentServer;

        private bool _capturing = false;

        public MainWindow Win { set; get; }

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);
       
        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);


        private IntPtr _hhook = IntPtr.Zero;
        private LowLevelMouseProc _mproc;
        private LowLevelKeyboardProc _proc;
        private IntPtr _mhhook = IntPtr.Zero;
        private Boolean _ctrlPressed;
        private Boolean _shiftPressed;
        private Boolean _winPressed;
        private Boolean _altPressed;

        private const int WH_KEYBOARD_LL = 13; 
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
       

        public Hooker()
        {
            
           // _conn.TcpConnectAndLogin("Administrator","WORKGROUP","admin");

        }

        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private struct MouseMessage
        {
            public int x;
            public int y;
            public IntPtr wParam;
            public short mouseData;
        }

        public void SetHook()
        {
            _proc = KeybdHookProc;
            _mproc = MouseHookProc;
            IntPtr hInstance = LoadLibrary("User32");
            _hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
            _mhhook = SetWindowsHookEx(WH_MOUSE_LL, _mproc, hInstance, 0);
            Mouse.OverrideCursor = Cursors.None;
        }

        public void UnHook()
        {
            UnhookWindowsHookEx(_hhook);
            UnhookWindowsHookEx(_mhhook);
        }

        private int _mmouselen = Marshal.SizeOf(typeof(MouseMessage));

        private IntPtr MouseHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                MouseMessage m = new MouseMessage();
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                double width = System.Windows.SystemParameters.PrimaryScreenWidth;
                double height = System.Windows.SystemParameters.PrimaryScreenHeight;
               // Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
                m.wParam = wParam;
                m.x = (int)((hookStruct.pt.x / width) * 65535);
                m.y = (int)((hookStruct.pt.y / height) * 65535);
                m.mouseData = (short)(hookStruct.mouseData >> 16);
                if (hookStruct.pt.x >= width && !_capturing && RightServer!=null)
                {
                    _currentServer = RightServer;
                    _capturing = true;
                    _currentServer.SendLocalClipboard();
                    mouse_event(1 | 0x8000, (uint) ((2/width)*65535), (uint) m.y,0,UIntPtr.Zero);
                  //  m.x = (int) ((2/width)*65535);
                    Win.Background =  new BrushConverter().ConvertFrom("#01000000") as Brush;
                    Win.Topmost = true;
                    Win.Activate();
                    return (IntPtr) 1;
                }

                if (hookStruct.pt.x <= 0 && _capturing)
                {
                    _capturing = false;
                    mouse_event(1 | 0x8000, (uint) (((width - 4)/width)*65535), (uint)m.y, 0, UIntPtr.Zero);
                   // m.x = (int)(((width - 2) / width) * 65535);
                    Win.Background = new BrushConverter().ConvertFrom("#00000000") as Brush;
                    _currentServer.GetRemoteClipboard();
                    return (IntPtr)1;
                }

                if (_capturing && _currentServer != null) 
                { 
                    
                    byte[] toSend = new byte[_mmouselen];
                    IntPtr ptr = Marshal.AllocHGlobal(_mmouselen);
                    Marshal.StructureToPtr(m,ptr,true);
                    Marshal.Copy(ptr,toSend,0,_mmouselen);
                    Marshal.FreeHGlobal(ptr);
                    _currentServer.Send(false,toSend);
                    if (wParam != (IntPtr)MouseMessages.WM_MOUSEMOVE)
                        return (IntPtr)1;
                }
            }
                return CallNextHookEx(_mhhook, code, (int)wParam, lParam);
        }

      



        private IntPtr KeybdHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (_capturing && _currentServer != null)
            {
                if (code >= 0)
                {
                    KBDLLHOOKSTRUCT aux;

                    aux = (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (KBDLLHOOKSTRUCT));



                    //  MessageBox.Show("You pressed vkCode: " + aux.vkCode);
                    // bool keyUp = (wParam == (IntPtr) WM_KEYUP);
                    // int vkCode = aux.vkCode;

                    byte[] keyUp = BitConverter.GetBytes((wParam == (IntPtr) WM_KEYUP));
                    byte[] vkCode = BitConverter.GetBytes(aux.vkCode);

                    byte[] toSend = new byte[keyUp.Length + vkCode.Length];
                    keyUp.CopyTo(toSend, 0);
                    vkCode.CopyTo(toSend, keyUp.Length);

                    _currentServer.Send(true, toSend);
                    string up;
                    if (wParam == (IntPtr) WM_KEYUP)
                    {
                        up = "keyup";
                    }
                    else
                    {
                        up = "keydown";
                    }
                    Console.WriteLine("Sent vkCode: " + aux.vkCode + " " + up);
                    return (IntPtr) 1;

                }
            }

                return CallNextHookEx(_hhook, code, (int) wParam, lParam);


        }


        private void CheckForModifiers(IntPtr wParam, KBDLLHOOKSTRUCT lParam)
        {
            if (wParam == (IntPtr) WM_KEYDOWN && (lParam.vkCode == 162 || lParam.vkCode == 163))
                _ctrlPressed = true;
            else if (wParam == (IntPtr) WM_KEYDOWN && (lParam.vkCode == 164 || lParam.vkCode == 165))
                _altPressed = true;
            else if (wParam == (IntPtr)WM_KEYDOWN && (lParam.vkCode == 160 || lParam.vkCode == 161))
                _shiftPressed = true;
            else if (wParam == (IntPtr)WM_KEYDOWN && (lParam.vkCode == 91 || lParam.vkCode == 92))
                _shiftPressed = true;
            else if (wParam == (IntPtr)WM_KEYUP && (lParam.vkCode == 162 || lParam.vkCode == 163))
                _ctrlPressed = false;
            else if (wParam == (IntPtr)WM_KEYUP && (lParam.vkCode == 164 || lParam.vkCode == 165))
                _altPressed = false;
            else if (wParam == (IntPtr)WM_KEYUP && (lParam.vkCode == 160 || lParam.vkCode == 161))
                _shiftPressed = false;
            else if (wParam == (IntPtr)WM_KEYUP && (lParam.vkCode == 91 || lParam.vkCode == 92))
                _shiftPressed = false;

        }
    }
}
