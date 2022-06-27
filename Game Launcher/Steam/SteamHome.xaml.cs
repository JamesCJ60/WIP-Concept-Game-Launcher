using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfAnimatedGif;
namespace Game_Launcher.Steam
{
    /// <summary>
    /// Interaction logic for SteamHome.xaml
    /// </summary>
    public partial class SteamHome : Page
    {

        public BitmapImage gameImage;
        public ImageBrush gameImageBrush;
        public static string path = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
        public string GameName = "";
        public string[] GameList = { "Returnal", "HZD" };
        public SteamHome()
        {
            InitializeComponent();

            imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-fill.png"));
            imgTime.Source = new BitmapImage(new Uri(path + "//Assets//Icons//time-line.png"));
            getBattery();
            getTime();

            lblBat.Text = batPercent;
            lblTime.Text = time;

            gameImage = new BitmapImage(new Uri(path + "//GameAssets//Returnal//icon.jpg", UriKind.Relative));
            gameImageBrush = new ImageBrush(gameImage);

            Game1BG.Background = gameImageBrush;

            gameImage = new BitmapImage(new Uri(path + "//GameAssets//HZD//icon.jpg", UriKind.Relative));
            gameImageBrush = new ImageBrush(gameImage);
            Game2BG.Background = gameImageBrush;

            GameName = findGameName(0);

            changeSelectedGame();

            //set up timer for sensor update
            DispatcherTimer sensor = new DispatcherTimer();
            sensor.Interval = TimeSpan.FromSeconds(2);
            sensor.Tick += Update_Tick;
            sensor.Start();

            //set up timer for key combo system
            DispatcherTimer checkKeyInput = new DispatcherTimer();
            checkKeyInput.Interval = TimeSpan.FromSeconds(0.15);
            checkKeyInput.Tick += KeyShortCuts_Tick;
            checkKeyInput.Start();
        }

        public static MediaPlayer mediaPlayer = new MediaPlayer();

        public static string batPercent = "";
        public static string time = "";

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);
        public static void Garbage_Collect()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);

            Thread.Sleep(1);

            long usedMemory = GC.GetTotalMemory(true);
        }

        void Update_Tick(object sender, EventArgs e)
        {
            getBattery();
            getTime();

            lblBat.Text = batPercent;
            lblTime.Text = time;

            Garbage_Collect();
        }

        public async void getBattery()
        {
            int batteryLife = 0;
            try
            {
                ManagementClass wmi = new ManagementClass("Win32_Battery");
                ManagementObjectCollection allBatteries = wmi.GetInstances();

                double batteryLevel = 0;

                foreach (var battery in allBatteries)
                {
                    batteryLevel = Convert.ToDouble(battery["EstimatedChargeRemaining"]);
                }
                batteryLife = (int)batteryLevel;
                batPercent = Convert.ToInt32(batteryLevel).ToString() + "%";
            }
            catch (Exception ex)
            {

            }
            if (batteryLife > 50)
            {
                imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-fill.png"));
            }
            if (batteryLife < 45)
            {
                imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-low-line.png"));
            }

        }

        private static void getTime()
        {
            //Get current time
            DateTime currentTime = DateTime.Now;
            time = currentTime.ToString("HH:mm");
        }
        public static string MediaPath = "";
        private static void playAudio(string audioPath)
        {
            mediaPlayer.Stop();
            mediaPlayer.Open(new Uri(audioPath));
            MediaPath = audioPath;
            mediaPlayer.MediaEnded += new EventHandler(Media_Ended);
            mediaPlayer.Play();
        }

        private static void Media_Ended(object sender, EventArgs e)
        {
            mediaPlayer.Open(new Uri(MediaPath));
            mediaPlayer.Play();
        }

        DoubleAnimation fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(0.5)),
        };

        DoubleAnimation fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromSeconds(0.5)),
        };

        private int sec = 1;

        private async Task StartAnimationForLabel1()
        {
            GameBG.BeginAnimation(OpacityProperty, fadeOut);
            await Task.Delay(500);
        }

        private async Task StartAnimationForLabel2()
        {
            GameBG.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(500);
        }

        public string findGameName(int button)
        {
            string name = "";
            int i = -1;
            do { i++; } while (i != button);

            if (i <= GameList.Length)
            {
                name = GameList[i];
            }

            if (!File.Exists(path + $"//GameAssets//{name}//background.jpg"))
            {
                return "default";
            }

            return name;
        }

        public async void changeSelectedGame()
        {
            await StartAnimationForLabel1();

            string url = "";
            if (File.Exists(path + $"//GameAssets//{GameName}//background.gif"))
            {
                url = path + $"//GameAssets//{GameName}//background.gif";
            }
            else
            {

                url = path + $"//GameAssets//{GameName}//background.jpg";
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.EndInit();
            ImageBehavior.SetAnimatedSource(GameBG, image);

            await StartAnimationForLabel2();

            playAudio(path + $"//GameAssets//{GameName}//audio.m4a");
        }

        private static Controller controller;

        private int MenuNumGameList = 0;
        private int MenuNumGameListLast = 0;
        void KeyShortCuts_Tick(object sender, EventArgs e)
        {

            //Get controller
            controller = new Controller(UserIndex.One);

            bool connected = controller.IsConnected;


            if (connected)
            {
                //get controller state
                var state = controller.GetState();
                SharpDX.XInput.Gamepad gamepad = controller.GetState().Gamepad;
                float tx = gamepad.LeftThumbX / (float)short.MaxValue;

                //detect if keyboard or controller combo is being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) || tx < 0)
                {
                    if(MenuNumGameList > 0)
                    {
                        MenuNumGameList--;
                    }

                    updateMenuGameList();
                }

                //detect if keyboard or controller combo is being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight) || tx > 0)
                {
                    if (MenuNumGameList < 1)
                    {
                        MenuNumGameList++;
                    }

                    updateMenuGameList();
                }
            }
        }

        private void updateMenuGameList()
        {
            if (MenuNumGameList == 0 && MenuNumGameListLast != 0)
            {
                Game1BG.BorderBrush = Brushes.LightBlue;
                Game2BG.BorderBrush = Brushes.Transparent;
            }
            if (MenuNumGameList == 1 && MenuNumGameListLast != 1)
            {
                Game1BG.BorderBrush = Brushes.Transparent;
                Game2BG.BorderBrush = Brushes.LightBlue;
            }

            if (MenuNumGameList != MenuNumGameListLast)
            {
                GameName = findGameName(MenuNumGameList);
                changeSelectedGame();
            }

            MenuNumGameListLast = MenuNumGameList;
        }

        private void Game1btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 0;
            updateMenuGameList();
        }

        private async void Game2btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 1;
            updateMenuGameList();
        }


        

    }
}
