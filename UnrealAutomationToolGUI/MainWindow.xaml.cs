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
using System.ComponentModel;

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
        //Process ubtProcess
        Process uatProcess;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void EngineDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog engineFolderDialog = new CommonOpenFileDialog("Engine directory");
            engineFolderDialog.IsFolderPicker = true;


            if (engineFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                EngineDirectoryTextBlock.Text = engineFolderDialog.FileName;
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
            // Cache our ui string variables
            string engineDirectory = EngineDirectoryTextBlock.Text;
            string uprojectPath = UProjectPathTextBlock.Text;
            string uprojectDirectory = uprojectPath.Remove(uprojectPath.LastIndexOf(".uproject"));
            string uprojectName = uprojectDirectory.Remove(0, uprojectDirectory.LastIndexOf('\\') + 1);


            //// Run Unreal Build Tool
            //
            //ubtProcess = new Process()
            //{
            //    StartInfo = new ProcessStartInfo()
            //    {
            //        FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\Build.bat",
            //        Arguments = $"{uprojectName}Editor Win64 Development \"{uprojectDirectory}\" -WaitMutex",
            //        WindowStyle = ProcessWindowStyle.Hidden
            //    }
            //};
            //
            //ubtProcess.StartInfo.UseShellExecute = false;
            //ubtProcess.StartInfo.RedirectStandardOutput = true;
            //ubtProcess.OutputDataReceived += (s, args) => Dispatcher.Invoke(() =>
            //{
            //    OutputTextBox.Text += args.Data + '\n';
            //});
            //ubtProcess.ErrorDataReceived += (s, args) => Dispatcher.Invoke(() =>
            //{
            //    OutputTextBox.Text += args.Data + '\n';
            //});
            //
            //if (File.Exists(ubtProcess.StartInfo.FileName) && ubtProcess.Start())
            //{
            //    ubtProcess.BeginOutputReadLine();
            //}


            // Run Unreal Automation Tool

            if (uatProcess != null && uatProcess.HasExited == false)
            {
                return;
            }

            uatProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\RunUAT.bat",
                    Arguments = $"BuildCookRun -Project=\"{uprojectPath}\" -NoP4 -NoCompileEditor -Distribution -TargetPlatform=Win64 -Platform=Win64 -ClientConfig=Shipping -ServerConfig=Shipping -Cook -Build -Stage -Pak -Archive -source -Prereqs -Package",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = false
                }
            };

            uatProcess.StartInfo.UseShellExecute = false;
            uatProcess.StartInfo.RedirectStandardOutput = true;
            uatProcess.OutputDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                OutputTextBox.Text += args.Data + '\n';
                OutputTextBox.ScrollToEnd();
            });
            uatProcess.ErrorDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                OutputTextBox.Text += args.Data + '\n';
                OutputTextBox.ScrollToEnd();
            });

            if (File.Exists(uatProcess.StartInfo.FileName) && uatProcess.Start())
            {
                uatProcess.BeginOutputReadLine();
            }
        }


        private void OnApplicationEnd(object sender, CancelEventArgs e)
        {
            //if (ubtProcess != null)
            //{
            //    ubtProcess.Kill(true);
            //}
            if (uatProcess != null)
            {
                uatProcess.Kill(true);
            }
        }
    }
}
