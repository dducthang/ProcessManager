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
using Label = System.Windows.Controls.Label;
using Brushes = System.Windows.Media.Brushes;

namespace ProcessesManager
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    class Schedule
    {
        public TimeSpan from;
        public TimeSpan to;
        public int duration;
        public int interupt;
        public int sum;
        public Schedule(string entry)
        {
            //"F07:30 T11:30 D60 I20 S150", "F12:30 T5:30 D60 I20 S150", "F19:30 T23:30 D60 I20 S150"

            string[] subEntry = entry.Split(' ');

            string timeFrom = subEntry[0].Substring(1);
            string[] timeFromEntry = timeFrom.Split(':');
            from = new TimeSpan(Int32.Parse(timeFromEntry[0]), Int32.Parse(timeFromEntry[1]), 0);

            string timeTo = subEntry[1].Substring(1);
            string[] timeToEntry = timeTo.Split(':');
            to = new TimeSpan(Int32.Parse(timeToEntry[0]), Int32.Parse(timeToEntry[1]), 0);

            if (!Int32.TryParse(subEntry[2].Substring(1), out duration))
            {
                duration = 0;
            }
            if (!Int32.TryParse(subEntry[3].Substring(1), out interupt))
            {
                duration = 0;
            }
            if (!Int32.TryParse(subEntry[4].Substring(1), out sum))
            {
                duration = 0;
            }
        }

        public void setFrom(string from_string)
        {
            string[] timeEntry = from_string.Split(':');
            from = new TimeSpan(Int32.Parse(timeEntry[0]), Int32.Parse(timeEntry[1]), 0);
        }

        public void setTo(string to_string)
        {
            string[] timeEntry = to_string.Split(':');
            from = new TimeSpan(Int32.Parse(timeEntry[0]), Int32.Parse(timeEntry[1]), 0);
        }

        public string ToEntry()
        {
            return 'F' + from.ToString(@"hh\:mm") + ' ' + 'T' + to.ToString(@"hh\:mm") + ' ' + 'D' + duration.ToString() + ' ' + 'I' + interupt.ToString() + ' ' + 'S' + sum.ToString();
        }

    }


    public partial class MainWindow : Window
    {
        private string[] defaultSchedule =
            {
            "F08:30 T11:30 D0060 I0020 S0150", "F12:30 T17:30 D0060 I0020 S0150", "F18:00 T23:30 D0003 I0020 S0150"
        };
        private string[] todaySchedule;
        private string todayPath;
        private string oneDrivePath = Environment.GetEnvironmentVariable("OneDriveConsumer") + @"\" + "os";
        private int pass_try = 3;
        
        private bool isLogin = false;
        private bool preventChildren = false;

        private TimeSpan LoggedInTime = new TimeSpan();
        private TimeSpan TimeLeft = new TimeSpan();
        private int stage = 0;
        Tuple<bool, string> check = null;
        private bool isScheduleChanged = false;
        private List<Schedule> _schedule = new List<Schedule>();
        FileSystemWatcher watcher;

        private string childPW;
        private string parentPW;

        //=============================================================================
       // private int curr_time = 0;

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
        /// Write pressed key into log.txt filethang dep trai vipor
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
            Console.WriteLine((Keys)vkCode);
            string oneDrive = Environment.GetEnvironmentVariable("OneDriveConsumer") + @"\" + "os";
            DateTime localDate = DateTime.Now;
            string todayPath = oneDrive + @"\management\" + localDate.ToString("dd-MM-yyyy");
            if (((Keys)vkCode).ToString() != "PrintScreen")
            {
                if (((Keys)vkCode).ToString() == "Space")
                {
                    File.AppendAllText(todayPath + @"\keylogger.txt", " ");
                }
                else
                {
                    File.AppendAllText(todayPath + @"\keylogger.txt", ((Keys)vkCode).ToString());
                }
            }

            /*Console.WriteLine((Keys)vkCode);
            string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
            StreamWriter sw = new StreamWriter(logNameToWrite, true);
            sw.Write((Keys)vkCode);
            sw.Close();*/
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

            //kiểm tra folder gốc tồn tại
            if (!Directory.Exists(oneDrivePath))
            {
                Directory.CreateDirectory(oneDrivePath);
            }
            //kiểm folder của ngày hôm nay
            DateTime localDate = DateTime.Now;
            todayPath = oneDrivePath + @"\management\" + localDate.ToString("dd-MM-yyyy");
            // nếu folder ngày hôm nay chưa tồn tại thì tạo
            if (!Directory.Exists(todayPath))
            {
                Directory.CreateDirectory(todayPath);
                Directory.CreateDirectory(todayPath + @"\" + "capture");
                File.Create(todayPath + @"\" + "keylogger.txt");
            }
            //kiểm tra file schedule nếu có thì đọc vào todaySchedule
            //nếu không có thì nhận defaultSchedule, tạo schedule.txt
            if (!File.Exists(todayPath + @"\schedule.txt"))
            {
                MakeDefaultScheduleTask();
                todaySchedule = defaultSchedule;
            }
            else
            {
                todaySchedule = File.ReadAllLines(todayPath + @"\schedule.txt");
                for (var i = 0; i < todaySchedule.Length; i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                }
            }
            if (File.Exists(oneDrivePath + @"\child-password.txt"))
            {
                childPW = File.ReadAllText(oneDrivePath + @"\child-password.txt");
            }
            if (File.Exists(oneDrivePath + @"\parent-password.txt"))
            {
                parentPW = File.ReadAllText(oneDrivePath + @"\parent-password.txt");
            }
            // kiểm tra đăng nhập thất bại và phải đang chờ 10'
            if (File.Exists("loginFaile.txt"))
            {
                string timeFaileString = File.ReadAllText("loginFaile.txt");
                TimeSpan timeFaile = TimeSpan.Parse(timeFaileString);
                TimeSpan current = DateTime.Now.TimeOfDay;
                int totalMinute = (int)Math.Round((current - timeFaile).TotalMinutes);
                if (totalMinute < 10)
                {
                    preventChildren = true;
                    var minutesLeft = new TimeSpan(0, 10-totalMinute, 0);
                    current = current + minutesLeft;
                    canvas.Children.Remove(StartBtn);
                    canvas.Children.Remove(PassTextBlock);
                    canvas.Children.Remove(LogInLabel);
                    Canvas.SetTop(noti, 170);
                    Canvas.SetLeft(noti, 150);
                    noti.Content += $"please comeback at {current.ToString(@"hh\:mm\:ss")}, \nafter {10 - totalMinute} minutes \nsystem will shutdown in 5 sec";
                    //terminate(5);
                    return;
                }
                else
                {
                    File.Delete("loginFaile.txt");
                }
            }
            // kiểm tra có trong thời gian cho phép của children k,
            // nếu không thì update thời gian quay lại vào notification
            //Tuple<bool, string> check = checkCurrentTime();
            check = checkCurrentTime();
            if (check.Item1)
            {
                loginTimer(0.25);
            }
            else
            {
                //noti.Content += $"Children can't use now, \n{check.Item2}";
                preventChildren = true;
                loginTimer(0.25);
            }
   
        }

        private void AskAgain()
        {
            isLogin = false;
            this.Show();
            loginTimer(0.3);
        }

        private void Capture()
        {
            SendKeys.SendWait("{PRTSC}");
            System.Drawing.Image myImage = Clipboard.GetImage();
            string img = DateTime.Now.TimeOfDay.ToString(@"hh\hmm\mss") + imgExtendtion;
            myImage.Save(todayPath + @"\" + "capture" + $@"\{img}");
        }

        private int count = 0;
        private int CheckTimeLeft()
        {
           
            if (isScheduleChanged)
            {
                //Nếu file Schedule.txt có sự thay đổi thì cập nhật danh sách _schedule
                _schedule.Clear();
                for (var i=0; i<todaySchedule.Length;i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                }
                isScheduleChanged = false;
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan currentDuration = new TimeSpan(0, _schedule[stage].duration, 0);
            TimeLeft = (LoggedInTime + currentDuration) - currentTime;
            time_left.Content = $"{TimeLeft.Minutes}-{TimeLeft.Seconds}--{count}";
            if (TimeLeft.Minutes <= 1)
            {
                LogShutdownTime().Wait();
                terminate(60);
                MessageBox.Show("This computer will be shutdowned in 1 min");
                return 1;
            }
            return 0;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {

            if (PassTextBlock.Password=="")
            {
                MessageBox.Show("Please enter your password");
            }
            else if (PassTextBlock.Password == childPW)
            {
                // kiểm tra children có được sử dụng máy hay k
                if (preventChildren)
                {
                    TimerLabel.Content = "System will be shutdowned in:";
                    /*canvas.Children.Remove(StartBtn);
                    canvas.Children.Remove(PassTextBlock);
                    canvas.Children.Remove(LogInLabel);
                    Canvas.SetTop(noti, 170);
                    Canvas.SetLeft(noti, 240);*/
                    noti.Content = $"Children can't use now\n{check.Item2}";
                    
                    return;
                }
                //this.Hide();
                isLogin = true;

                //lưu lại thời điểm đăng nhập vào hệ thống
                LoggedInTime = DateTime.Now.TimeOfDay;

                for (var i = 0; i < todaySchedule.Length; i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                    if (LoggedInTime >= time.from && LoggedInTime <= time.to)
                    {
                        //Xác định thời điểm đăng nhập đang ở giai đoạn nào 
                        stage = i;
                    }
                }
                runInWatchChildren();
            }
            else if (PassTextBlock.Password == parentPW)
            {
                CancelTemins();
                isLogin = true;
                //this.Hide();
                runInWatchParrent();
            }
            else
            {
                pass_try--;
                if (pass_try > 0)
                {
                    MessageBox.Show($"Wrong password, {pass_try} tries(try) left.");
                }
                else
                {
                    MessageBox.Show($"Your computer will shutdown now. Please try again after 10 minutes");
                    TimeSpan loginF = DateTime.Now.TimeOfDay;
                    File.WriteAllText("loginFaile.txt", loginF.ToString());
                    //temins(0);
                }

            } 
        }

        // main method 
        public void runInWatchParrent()
        {
            //============================Tiến trình yêu cầu đăng nhập lại sau 60 phút
            Thread askAgain = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(60000);
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        AskAgain();
                    });
                }
            });

            askAgain.IsBackground = true;
            askAgain.Start();
            
            MessageBox.Show("parent mode start");
        }

        private void runInWatchChildren()
        {
            //===================Tiến trình hook keyboard
            Thread keyLogger = new Thread(() =>
            {
                HookKeyboard();
                /*ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                    HookKeyboard();
                });*/
            });
            keyLogger.IsBackground = true;
            keyLogger.Start();

            //=================Tiến trình chụp màn hình
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
            capTure.IsBackground = true;
            capTure.Start();


            //==================Tiến trình kiểm tra thời gian sử dùng còn lại của trẻ
            int breakLoops = 0;
            Thread checkLeftTime = new Thread(() =>
            {
                while (true)
                {
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        breakLoops = CheckTimeLeft();
                    });
                    Thread.Sleep(1000);
                    if (breakLoops == 1)
                        break;
                }
            });
            checkLeftTime.IsBackground = true;
            checkLeftTime.Start();


            //======================Tiến trình kiểm tra thay đổi ở file Schedule.txt
            watcher = new FileSystemWatcher();
            watcher.Path = todayPath;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "schedule.txt";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            MessageBox.Show("children mode start");
        }

        
        public void loginTimer(double minute)
        {
            Thread timer = new Thread(() =>
            {
                int curr_time = (int)Math.Round(60000 * minute);
                while (curr_time > 0)
                {
                    curr_time -= 1000;
                    this.Dispatcher.Invoke(() =>
                    {
                        lb_timer.Content = $"{curr_time / 1000} ";
                    });

                    Thread.Sleep(1000);
                }
                if (!isLogin)
                {
                    MessageBox.Show("bummmmmm... shut down");
                    //temins(0);
                }
            });
            timer.IsBackground = true;
            timer.Start();
        }
        // utility method
        public async Task MakeDefaultScheduleTask()
        {
            await File.WriteAllLinesAsync(todayPath + @"\schedule.txt", defaultSchedule);
        }
        
        public async Task LogShutdownTime()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            string shutdownTimeStr = $"{now.Hours}:{now.Minutes}:{now.Seconds}";
            try
            {
                await File.WriteAllTextAsync(todayPath+ @"\shutdownTime.txt", shutdownTimeStr);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show(e.ToString());
            }
        }

        public async Task updateScheduleTask()
        {
            if (todaySchedule.Length > 0)
            {
                await File.WriteAllLinesAsync(todayPath + @"\schedule.txt", todaySchedule);
            }
            else
            {
                return;
            }
        }

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            while (true) { 
                try
                {
                    todaySchedule = File.ReadAllLines(todayPath + @"\schedule.txt");
                    isScheduleChanged = true;
                    //MessageBox.Show("Schedule change");
                }
                catch (IOException)
                {
                    
                }
            }
           
        }

        // shutdown máy sau "sec" giây
        public void terminate(int sec)
        {
            var psi = new ProcessStartInfo("shutdown", $"/s /t {sec}");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        // hủy shutdown máy
        public void CancelTemins()
        {
            var psi = new ProcessStartInfo("shutdown", "-a");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        // kiểm tra thời gian hiện tại có nằm trong schedule không
        public Tuple<bool,string> checkCurrentTime()
        {
            TimeSpan curr = DateTime.Now.TimeOfDay;
            TimeSpan nextAccessTime = new TimeSpan(0,0,0);

            for (int i = 0; i < todaySchedule.Length; i++)
            {
                Schedule time = new Schedule(todaySchedule[i]);
                if (curr >= time.from && curr <= time.to)
                {
                    //Nếu tồn tại file shutdown (ghi lại thời điểm máy shutdown gần nhất) thì tính thời gian hiện tại cộng với intterupt time xem đã đủ để mở lại chưa
                    if(File.Exists(todayPath + @"\shutdownTime.txt"))
                    {
                        string shutdownTimeStr= File.ReadAllText(todayPath+ @"\shutdownTime.txt");
                        string[] timeParts = shutdownTimeStr.Split(':');
                        TimeSpan shutdownTime = new TimeSpan(Int32.Parse(timeParts[0]), Int32.Parse(timeParts[1]), Int32.Parse(timeParts[2]));
                        TimeSpan interruptTime = new TimeSpan(0, time.interupt, 0);
                        nextAccessTime = shutdownTime + interruptTime;
                    }
                    if (nextAccessTime <= curr)
                    {
                        return Tuple.Create(true, "come back at " + time.to.ToString(@"hh\:mm\:ss"));
                    }
                    return Tuple.Create(false, "come back at " + nextAccessTime.ToString(@"hh\:mm\:ss"));
                }
                else if (curr < time.from)
                {
                    return Tuple.Create(false, "come back at " + time.from.ToString(@"hh\:mm\:ss"));
                }
                else if(i == todaySchedule.Length -1 && curr > time.to)
                {
                    return Tuple.Create(false, "no more computer today");
                }
                
            }
            return Tuple.Create(false, "not found");
        }

        // thêm sự kiện nhấn enter
        private void tb_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key.ToString() == "Return")
            {
                StartBtn_Click(this,new RoutedEventArgs());
            }
        }
    }
}
