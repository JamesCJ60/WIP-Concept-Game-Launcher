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
        //Varibales for game button border background
        public BitmapImage gameImage;
        public ImageBrush gameImageBrush;

        //Get current working directory
        public static string path = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
        
        //Variables for steam games
        public string GameName = "";
        public static string[] GameList = FindSteamGames.GameNames;
        public static string[] SteamGameIDs = FindSteamGames.GameIDs;
        
        //Variables for mediaplayer
        private static MediaPlayer mediaPlayer = new MediaPlayer();
        public static string MediaPath = "";

        //Variables for time and battery
        public static string batPercent = "";
        public static string time = "";

        //Variable for controller
        private static Controller controller;

        //Variables for selected and last selected menu
        private int MenuNumGameList = 0;
        private int MenuNumGameListLast = 0;

        //Save if window was out of focus
        private bool wasNotFocused = false;


        public SteamHome()
        {
            InitializeComponent();

            //Get game list
            updateGameList();

            //set battery and time icon
            imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-fill.png"));
            imgTime.Source = new BitmapImage(new Uri(path + "//Assets//Icons//time-line.png"));

            //get current battery and time
            getBattery();
            getTime();

            lblBat.Text = batPercent;
            lblTime.Text = time;

            //get game images
            updateGameImages();

            //Set first game name
            GameName = findGameName(0);

            //Set selected game to 0
            changeSelectedGame();

            //set up timer for sensor update
            DispatcherTimer sensor = new DispatcherTimer();
            sensor.Interval = TimeSpan.FromSeconds(2);
            sensor.Tick += Update_Tick;
            sensor.Start();

            //set up timer for controller input
            DispatcherTimer checkKeyInput = new DispatcherTimer();
            checkKeyInput.Interval = TimeSpan.FromSeconds(0.15);
            checkKeyInput.Tick += KeyShortCuts_Tick;
            checkKeyInput.Start();

            //set up timer for controller input
            DispatcherTimer joystickInput = new DispatcherTimer();
            checkKeyInput.Interval = TimeSpan.FromSeconds(0.25);
            checkKeyInput.Tick += joystickInput_Tick;
            checkKeyInput.Start();

            lblGameName.Text = GameList[0].ToString();
        }

        private void updateGameList()
        {
            GameList = FindSteamGames.GameNames;
            SteamGameIDs = FindSteamGames.GameIDs;
        }

        //Update game image of game button borders
        private void updateGameImages()
        {
            //Arrays containing button borders and text blocks
            var buttonBG = new[] { Game1BG, Game2BG, Game3BG, Game4BG, Game5BG, Game6BG, Game7BG, Game8BG, Game9BG };
            var buttonText = new[] { Game1lbl, Game2lbl, Game3lbl, Game4lbl, Game5lbl, Game6lbl, Game7lbl, Game8lbl, Game9lbl };
            int i = 0;
            string newGameName = "";
            SolidColorBrush borderBG;

            //Go through every game and assign an image or show textblock
            foreach(string game in GameList)
            {
                //Remove : from game names for url
                newGameName = game.Replace(":", "");
                
                //Reset the test of the buttons
                if(i < buttonText.Length)
                {
                    buttonText[i].Text = "";
                }

                //Check if jpeg icon exists
                if (File.Exists(path + $"//GameAssets//{newGameName}//icon.jpeg"))
                {
                    //Set background to icon image
                    gameImage = new BitmapImage(new Uri(path + $"//GameAssets//{newGameName}//icon.jpeg", UriKind.Relative));
                    gameImageBrush = new ImageBrush(gameImage);
                    buttonBG[i].Background = gameImageBrush;
                }
                //Check if jpg icon exists
                else if (File.Exists(path + $"//GameAssets//{newGameName}//icon.jpg"))
                {
                    //Set background to icon image
                    gameImage = new BitmapImage(new Uri(path + $"//GameAssets//{newGameName}//icon.jpg", UriKind.Relative));
                    gameImageBrush = new ImageBrush(gameImage);
                    buttonBG[i].Background = gameImageBrush;
                }
                //If no icon exists show text with game name
                else
                {
                    //set border background to black with an ~85% opacity
                    borderBG = new SolidColorBrush(Color.FromArgb(216, 0, 0, 0));
                    buttonBG[i].Background = borderBG;

                    //Set game name and align text in center
                    buttonText[i].Text = game;
                    buttonText[i].TextAlignment = TextAlignment.Center;
                }
                i++;
            }
        }



        //garbage collection
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);
        public void Garbage_Collect()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);

            long usedMemory = GC.GetTotalMemory(true);
        }

        //Get battery and time info evry 2 seconds
        void Update_Tick(object sender, EventArgs e)
        {
            getBattery();
            getTime();

            //Update battery and time text blocks
            lblBat.Text = batPercent;
            lblTime.Text = time;

            //Run forced garbage collection to free up system
            Garbage_Collect();
        }

        //Pull battery sensor info from Windows
        public void getBattery()
        {
            int batteryLife = 0;
            try
            {
                ManagementClass wmi = new ManagementClass("Win32_Battery");
                ManagementObjectCollection allBatteries = wmi.GetInstances();

                double batteryLevel = 0;

                //Get battery level from each system battery detected
                foreach (var battery in allBatteries)
                {
                    batteryLevel = Convert.ToDouble(battery["EstimatedChargeRemaining"]);
                }
                //Set battery level as an int
                batteryLife = (int)batteryLevel;

                //Update battery level string
                batPercent = batteryLife.ToString() + "%";

                //Update battery icon based on battery level
                if (batteryLife > 50)
                {
                    imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-fill.png"));
                }
                if (batteryLife < 45)
                {
                    imgBat.Source = new BitmapImage(new Uri(path + "//Assets//Icons//battery-low-line.png"));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void getTime()
        {
            //Get current time
            DateTime currentTime = DateTime.Now;
            time = currentTime.ToString("HH:mm");
        }


        
        //Update media player with current game BG music 
        private void playAudio(string audioPath)
        {
            //Stop current music 
            mediaPlayer.Stop();
            //Open new game music
            mediaPlayer.Open(new Uri(audioPath));
            MediaPath = audioPath;
            //Make sure music repeats on end
            mediaPlayer.MediaEnded += new EventHandler(Media_Ended);
            //Set volume to 90%
            mediaPlayer.Volume = 0.9;
            //Play music
            mediaPlayer.Play();
        }

        private void Media_Ended(object sender, EventArgs e)
        {
            //Restart music play back
            mediaPlayer.Open(new Uri(MediaPath));
            mediaPlayer.Play();
        }

        //Fade out animation
        DoubleAnimation fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(0.8)),
        };

        //Fade in animation
        DoubleAnimation fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromSeconds(0.8)),
        };

        //background fade out animation
        private async Task StartAnimationForLabel1()
        {
            GameBG.BeginAnimation(OpacityProperty, fadeOut);
            await Task.Delay(850);
        }

        //background fade in animation
        private async Task StartAnimationForLabel2()
        {
            GameBG.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(850);
        }


        //Find game name for button
        public string findGameName(int button)
        {
            string name = "";
            int i = button;

            //Make sure button exists
            if (i <= GameList.Length)
            {
                name = GameList[i];
                name = name.Replace(":", "");
            }

            //Find check game background exists
            if (!File.Exists(path + $"//GameAssets//{name}//background.jpg") && !File.Exists(path + $"//GameAssets//{name}//background.jpeg") && !File.Exists(path + $"//GameAssets//{name}//background.gif"))
            {
                return "default";
            }

            //Return game game
            return name;
        }

        //Change background image and music 
        public async void changeSelectedGame()
        {
            //Start animation
            await StartAnimationForLabel1();

            string url = "";

            //Remove : from any game names
            string newGameName = GameName.Replace(":", "");

            //Check if BG gif exists and save url
            if (File.Exists(path + $"//GameAssets//{newGameName}//background.gif"))
            {
                url = path + $"//GameAssets//{newGameName}//background.gif";
            }
            //Check if BG jpeg exists and save url
            else if (File.Exists(path + $"//GameAssets//{newGameName}//background.jpeg"))
            {
                url = path + $"//GameAssets//{newGameName}//background.jpeg";
            }
            //Use BG jpg and save url
            else
            {
                url = path + $"//GameAssets//{newGameName}//background.jpg";
            }

            //Save new image and load it
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.EndInit();
            //Set BG image
            ImageBehavior.SetAnimatedSource(GameBG, image);
            //Start fade in animation
            await StartAnimationForLabel2();

            //Play new game audio
            playAudio(path + $"//GameAssets//{newGameName}//audio.m4a");
        }

        void KeyShortCuts_Tick(object sender, EventArgs e)
        {
            //If window is not focused stop music
            if(MainWindow.ApplicationIsActivated() != true)
            {
                mediaPlayer.Pause();
                wasNotFocused = true;
            }
            //If window is now focused resume music
            else if (MainWindow.ApplicationIsActivated() == true && wasNotFocused == true)
            {
                mediaPlayer.Play();
                wasNotFocused = false;
            }


            //Get controller
            controller = new Controller(UserIndex.One);

            //Make sure controller is connected
            bool connected = controller.IsConnected;


            if (connected && wasNotFocused == false)
            {
                //get controller state
                var state = controller.GetState();
                SharpDX.XInput.Gamepad gamepad = controller.GetState().Gamepad;

                //detect if select controller input has been being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
                {
                    //Move down one
                    if(MenuNumGameList > 0)
                    {
                        MenuNumGameList--;
                    }

                    //Scroll scrollviewer down
                    OnScrollDown();
                    //Update menu to new select button
                    updateMenuGameList();
                }

                //detect if keyboard or controller combo is being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
                {
                    //Move up one
                    if (MenuNumGameList < (GameList.Length - 1))
                    {
                        MenuNumGameList++;
                    }
                    //Scroll scrollviewer up
                    OnScrollUp();
                    //Update menu to new select button
                    updateMenuGameList();
                }

                //detect if select controller input has been being activated
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                {
                    //Start selected game
                    string steamLaunch = "steam://rungameid/" + SteamGameIDs[MenuNumGameList];
                    if (SteamGameIDs[MenuNumGameList] != "N/A") System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Steam\steam.exe", steamLaunch);
                }

                //Get right thumb pos
                float trx = gamepad.RightThumbX;

                //Scroll up
                if (trx > 18000)
                {
                    OnScrollUp();
                }

                //Scroll down
                if (trx < -18000)
                {
                    OnScrollDown();
                }
            }

            //Update game name label 
            double width = lblGameName.ActualWidth;
            width = lblGameName.ActualWidth;
            GameNameBar.Width = (width + 25);
        }

        void joystickInput_Tick(object sender, EventArgs e)
        {

            //Get controller
            controller = new Controller(UserIndex.One);

            //Make sure controller is connected
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
                    //Scroll scrollviewer down
                    OnScrollDown();
                    //Update menu to new select button
                    updateMenuGameList();
                }

                //detect if keyboard or controller combo is being activated
                if (tx > 18000)
                {
                    if (MenuNumGameList < (GameList.Length - 1))
                    {
                        MenuNumGameList++;
                    }

                    //Scroll scrollviewer up
                    OnScrollUp();
                    //Update menu to new select button
                    updateMenuGameList();
                }

                
            }

            //Update game name label 
            double width = lblGameName.ActualWidth;
            width = lblGameName.ActualWidth;
            GameNameBar.Width = (width + 28);
        }

        //Scroll scrollviewer up
        private void OnScrollUp()
        {
            var offset = MainScroll.ScrollableWidth / (GameList.Length - 2.975);

            MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset + offset);

        }

        //Scroll scrollviewer down
        private void OnScrollDown()
        {
            var offset = MainScroll.ScrollableWidth / (GameList.Length - 2.975);
            MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset - offset);

        }

        //Update selected game button
        private void updateMenuGameList()
        {
            var buttons = new[] { Game1BG, Game2BG, Game3BG, Game4BG, Game5BG, Game6BG, Game7BG, Game8BG, Game9BG };
            int i = 0;

            //Check to see selected game has changed
            if (MenuNumGameList != MenuNumGameListLast)
            {
                //Update game name
                lblGameName.Text = GameList[MenuNumGameList].ToString();

                //Set all borders to transparent
                foreach (Border button in buttons)
                {
                    button.BorderBrush = Brushes.Transparent;
                }

                //Update the border of selected button
                buttons[MenuNumGameList].BorderBrush = Brushes.LightBlue;
                GameName = findGameName(MenuNumGameList);

                //Update game BG and music
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
