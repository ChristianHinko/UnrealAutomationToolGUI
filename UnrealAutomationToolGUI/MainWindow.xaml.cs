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
using System.IO;

namespace UnrealAutomationToolGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Todo:
    ///     - Add argument buttons that add to args list for uatProcess
    ///     - Pretty up UI (leaving it ugly right now so we can get an MVP)
    ///     - Have it remember your path to engine and uproject
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

            CommonOpenFileDialog engineFolderDialog = new CommonOpenFileDialog();
            engineFolderDialog.IsFolderPicker = true;


            if (engineFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                EnginePathTextBlock.Text = engineFolderDialog.FileName;
            }
        }
        private void UProjectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog uprojectFolderDialog = new CommonOpenFileDialog();
            uprojectFolderDialog.Filters.Add(new CommonFileDialogFilter("uproject file", ".uproject"));


            if (uprojectFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                UProjectPathTextBlock.Text = uprojectFolderDialog.FileName;
            }
        }
        private void BuildBtn_Click(object sender, RoutedEventArgs e)
        {
            // Run Unreal Automation Tool

            Process uatProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{EnginePathTextBlock.Text}\\Engine\\Build\\BatchFiles\\RunUAT.bat",
                    Arguments = $"BuildCookRun -Project={UProjectPathTextBlock.Text} -NoP4 -NoCompileEditor -Distribution -TargetPlatform=Win64 -Platform=Win64 -ClientConfig=Shipping -ServerConfig=Shipping -Cook -Map=List+Of+Maps+To+Include -Build -Stage -Pak -Archive -ArchiveDirectory=<ArchivePath> -source -Prereqs -Package",
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            uatProcess.StartInfo.UseShellExecute = false;
            uatProcess.StartInfo.RedirectStandardOutput = true;
            uatProcess.OutputDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                UATOutputTextBox.Text += args.Data + '\n';
            });
            uatProcess.ErrorDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                UATOutputTextBox.Text += args.Data + '\n';
            });

            if (File.Exists(uatProcess.StartInfo.FileName) && uatProcess.Start())
            {
                uatProcess.BeginOutputReadLine();
            }
        }
    }
}
