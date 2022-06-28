using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Game_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            PagesNavigation.Navigate(new System.Uri("Steam/SteamHome.xaml", UriKind.RelativeOrAbsolute));
        }

        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
    }
}
