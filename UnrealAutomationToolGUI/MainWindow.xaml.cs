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

namespace UnrealAutomationToolGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void EnginePathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialouge to select folder
        }
        private void BuildBtn_Click(object sender, RoutedEventArgs e)
        {
            // Run UAT (/Engine/Build/BatchFiles/RunUAT.bat)
        }
    }
}
