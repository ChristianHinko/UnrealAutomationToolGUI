using Microsoft.Win32;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;

namespace UnrealAutomationToolGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Todo:
    ///     - Add output text block that shows output of UAT
    ///     - Add argument buttons that add to args list for uatProcess
    ///     - Pretty up UI (leaving it ugly right now so we can get an MVP)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void EnginePathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog engineFolderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            engineFolderDialog.ShowDialog();

            EnginePathTextBlock.Text = engineFolderDialog.FileName;
        }
        private void BuildBtn_Click(object sender, RoutedEventArgs e)
        {
            // Run Unreal Automation Tool

            Process uatProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{EnginePathTextBlock.Text}\\Engine\\Build\\BatchFiles\\RunUAT.bat"
                }
            };

            // Something along the lines of this
            //if (uatProcess.Start())
            //{
            //
            //}
        }
    }
}
