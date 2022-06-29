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
        public static string[] GameList = { "Returnal", "Horizon: Zero Dawn", "Doom Eternal", "Cyberpunk 2077", "Hollow Knight", "Fallout 76", "Firewatch", "God of War", "Control" };
        public static string[] SteamGameIDs = { "N/A", "1151640", "782330", "1091500", "367520", "1151340", "383870", "1593500", "275147" };
        public SteamHome()
        {
            InitializeComponent();

            imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-fill.png"));
            imgTime.Source = new BitmapImage(new Uri(path + "//Assets//Icons//time-line.png"));
            getBattery();
            getTime();

            lblBat.Text = batPercent;
            lblTime.Text = time;

            updateGameImages();

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

            //set up timer for key combo system
            DispatcherTimer joystickInput = new DispatcherTimer();
            checkKeyInput.Interval = TimeSpan.FromSeconds(0.25);
            checkKeyInput.Tick += joystickInput_Tick;
            checkKeyInput.Start();

            lblGameName.Text = GameList[0].ToString();
        }


        private void updateGameImages()
        {
            var buttonBG = new[] { Game1BG, Game2BG, Game3BG, Game4BG, Game5BG, Game6BG, Game7BG, Game8BG, Game9BG };
            var buttonText = new[] { Game1lbl, Game2lbl, Game3lbl, Game4lbl, Game5lbl, Game6lbl, Game7lbl, Game8lbl, Game9lbl };
            int i = 0;
            string newGameName = "";
            SolidColorBrush borderBG;
            foreach(string game in GameList)
            {

                newGameName = game.Replace(":", "");
                
                if(i < buttonText.Length)
                {
                    buttonText[i].Text = "";
                }

                if (File.Exists(path + $"//GameAssets//{newGameName}//icon.jpeg"))
                {
                    gameImage = new BitmapImage(new Uri(path + $"//GameAssets//{newGameName}//icon.jpeg", UriKind.Relative));
                    gameImageBrush = new ImageBrush(gameImage);
                    buttonBG[i].Background = gameImageBrush;
                }
                else if (File.Exists(path + $"//GameAssets//{newGameName}//icon.jpg"))
                {
                    gameImage = new BitmapImage(new Uri(path + $"//GameAssets//{newGameName}//icon.jpg", UriKind.Relative));
                    gameImageBrush = new ImageBrush(gameImage);
                    buttonBG[i].Background = gameImageBrush;
                }
                else
                {
                    borderBG = new SolidColorBrush(Color.FromArgb(98, 0, 0, 0));
                    buttonText[i].Text = game;
                    buttonText[i].TextAlignment = TextAlignment.Center;
                    buttonBG[i].Background = borderBG;
                }



                
                i++;
            }
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

        public void getBattery()
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
            mediaPlayer.Volume = 0.9;
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

            if (i < GameList.Length)
            {
                name = GameList[i];
                name = name.Replace(":", "");
            }

            if (!File.Exists(path + $"//GameAssets//{name}//background.jpg") && !File.Exists(path + $"//GameAssets//{name}//background.jpeg") && !File.Exists(path + $"//GameAssets//{name}//background.gif"))
            {
                return "default";
            }

            return name;
        }

        public async void changeSelectedGame()
        {
            await StartAnimationForLabel1();

            string url = "";

            string newGameName = GameName.Replace(":", "");

            if (File.Exists(path + $"//GameAssets//{newGameName}//background.gif"))
            {
                url = path + $"//GameAssets//{newGameName}//background.gif";
            }
            else if (File.Exists(path + $"//GameAssets//{newGameName}//background.jpeg"))
            {
                url = path + $"//GameAssets//{newGameName}//background.jpeg";
            }
            else
            {
                url = path + $"//GameAssets//{newGameName}//background.jpg";
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.EndInit();
            ImageBehavior.SetAnimatedSource(GameBG, image);

            await StartAnimationForLabel2();

            playAudio(path + $"//GameAssets//{newGameName}//audio.m4a");
        }

        private static Controller controller;

        private int MenuNumGameList = 0;
        private int MenuNumGameListLast = 0;

        private bool wasNotFocused = false;
        void KeyShortCuts_Tick(object sender, EventArgs e)
        {
            if(MainWindow.ApplicationIsActivated() != true)
            {
                mediaPlayer.Pause();
                wasNotFocused = true;
            }
            else if(MainWindow.ApplicationIsActivated() == true && wasNotFocused == true)
            {
                mediaPlayer.Play();
                wasNotFocused = false;
            }


            //Get controller
            controller = new Controller(UserIndex.One);

            bool connected = controller.IsConnected;


            if (connected && wasNotFocused == false)
            {
                //get controller state
                var state = controller.GetState();
                SharpDX.XInput.Gamepad gamepad = controller.GetState().Gamepad;

                //detect if keyboard or controller combo is being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
                {
                    if(MenuNumGameList > 0)
                    {
                        MenuNumGameList--;
                    }

                    OnScrollDown();
                    updateMenuGameList();
                }

                //detect if keyboard or controller combo is being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
                {
                    if (MenuNumGameList < (GameList.Length - 1))
                    {
                        MenuNumGameList++;
                    }
                    OnScrollUp();
                    updateMenuGameList();
                }

                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                {
                    string steamLaunch = "steam://rungameid/" + SteamGameIDs[MenuNumGameList];
                    if (SteamGameIDs[MenuNumGameList] != "N/A") System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Steam\steam.exe", steamLaunch);
                }

                float trx = gamepad.RightThumbX;

                if (trx > 18000)
                {
                    OnScrollUp();
                }

                if (trx < -18000)
                {
                    OnScrollDown();
                }
            }

            double width = lblGameName.ActualWidth;
            width = lblGameName.ActualWidth;
            GameNameBar.Width = (width + 25);
        }

        void joystickInput_Tick(object sender, EventArgs e)
        {

            //Get controller
            controller = new Controller(UserIndex.One);

            bool connected = controller.IsConnected;


            if (connected && wasNotFocused == false)
            {
                //get controller state
                var state = controller.GetState();
                SharpDX.XInput.Gamepad gamepad = controller.GetState().Gamepad;
                float tx = gamepad.LeftThumbX;
                

                //detect if keyboard or controller combo is being activated
                if (tx < -18000)
                {
                    if (MenuNumGameList > 0)
                    {
                        MenuNumGameList--;
                    }

                    OnScrollDown();
                    updateMenuGameList();
                }

                //detect if keyboard or controller combo is being activated
                if (tx > 18000)
                {
                    if (MenuNumGameList < (GameList.Length - 1))
                    {
                        MenuNumGameList++;
                    }

                    OnScrollUp();
                    updateMenuGameList();
                }

                
            }

            double width = lblGameName.ActualWidth;
            width = lblGameName.ActualWidth;
            GameNameBar.Width = (width + 28);
        }

        private void OnScrollUp()
        {
            var offset = MainScroll.ScrollableWidth / (GameList.Length - 2.975);

            MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + offset);

        }

        private void OnScrollDown()
        {
            var offset = MainScroll.ScrollableWidth / (GameList.Length - 2.975);
            MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset - offset);

        }

        private void updateMenuGameList()
        {
            var buttons = new[] { Game1BG, Game2BG, Game3BG, Game4BG, Game5BG, Game6BG, Game7BG, Game8BG, Game9BG };
            int i = 0;

            if (MenuNumGameList != MenuNumGameListLast)
            {
                lblGameName.Text = GameList[MenuNumGameList].ToString();
                foreach (Border button in buttons)
                {
                    button.BorderBrush = Brushes.Transparent;
                }


                buttons[MenuNumGameList].BorderBrush = Brushes.LightBlue;
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

        private void Game2btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 1;
            updateMenuGameList();
        }

        private void Game3btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 2;
            updateMenuGameList();
        }

        private void Game4btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 3;
            updateMenuGameList();
        }

        private void Game5btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 4;
            updateMenuGameList();
        }

        private void Game6btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 5;
            updateMenuGameList();
        }

        private void Game7btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 6;
            updateMenuGameList();
        }

        private void Game8btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 7;
            updateMenuGameList();
        }
        private void Game9btn_Click(object sender, RoutedEventArgs e)
        {
            MenuNumGameList = 8;
            updateMenuGameList();
        }
    }
}
