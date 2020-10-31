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
using UnrealAutomationToolGUI.Properties;

namespace UnrealAutomationToolGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Todo:
    ///     - Add argument buttons that add to args list for uatProcess
    ///     - Pretty up UI (leaving it ugly right now so we can get an MVP)
    /// </summary>
    public partial class MainWindow : Window
    {
        Process ubtProcess;
        Process uatProcess;

        public MainWindow()
        {
            InitializeComponent();

            EngineDirectoryTextBox.Text = Settings.Default.EngineDirectory;
            UProjectPathTextBox.Text = Settings.Default.UProjectPath;
        }

        private void EngineDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog engineFolderDialog = new CommonOpenFileDialog("Engine directory");
            engineFolderDialog.IsFolderPicker = true;


            if (engineFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                EngineDirectoryTextBox.Text = engineFolderDialog.FileName;
                Settings.Default.EngineDirectory = EngineDirectoryTextBox.Text;
                Settings.Default.Save();
            }
        }
        private void UProjectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog uprojectFolderDialog = new CommonOpenFileDialog();
            uprojectFolderDialog.Filters.Add(new CommonFileDialogFilter("uproject file", ".uproject"));


            if (uprojectFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                UProjectPathTextBox.Text = uprojectFolderDialog.FileName;
                Settings.Default.UProjectPath = UProjectPathTextBox.Text;
                Settings.Default.Save();
            }
        }
        private void BuildBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cache our ui variables
            string engineDirectory = EngineDirectoryTextBox.Text;
            string uprojectPath = UProjectPathTextBox.Text;
            string uprojectDirectory = uprojectPath.Remove(uprojectPath.LastIndexOf(".uproject"));
            string uprojectName = uprojectDirectory.Remove(0, uprojectDirectory.LastIndexOf('\\') + 1);


            // Run Unreal Build Tool

            ubtProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\Build.bat",
                    Arguments = $"{uprojectName}Editor Win64 Development \"{uprojectPath}\" -WaitMutex",
                    //Arguments = $"\"C:\\Users\\Christian Hinkle\\Documents\\Unreal Projects\\SonicShooter\\SonicShooter\\SonicShooter.uproject\" \"C:\\Users\\Christian Hinkle\\Documents\\Unreal Projects\\SonicShooter\\SonicShooter\\Intermediate\\Build\\Win64\\SonicShooterEditor\\DebugGame\\SonicShooterEditor.uhtmanifest\" -LogCmds=\"loginit warning, logexit warning, logdatabase error\" -Unattended -WarningsAsErrors -abslog=\"C:\\Users\\Christian Hinkle\\Documents\\UE4\\Engine\\Programs\\UnrealBuildTool\\Log_UHT.txt\"", // i forgot what this was
                    //Arguments = $"-Target=\"SonicShooterEditor Win64 DebugGame - Project =\"{uprojectPath}\"\" - Target = \"ShaderCompileWorker Win64 Development -Quiet\" - WaitMutex - FromMsBuild", // What VS runs
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            ubtProcess.StartInfo.UseShellExecute = false;
            ubtProcess.StartInfo.RedirectStandardOutput = true;
            ubtProcess.OutputDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                OutputTextBox.Text += args.Data + '\n';

                //Paragraph goodParagraph = new Paragraph()
                //{
                //    Foreground = Brushes.Black
                //};
                //goodParagraph.Inlines.Add(new Run(args.Data + '\n'));
                //
                //OutputFlowDocument.Blocks.Add(goodParagraph);

                OutputTextBox.ScrollToEnd();
            });
            ubtProcess.ErrorDataReceived += (s, args) => Dispatcher.Invoke(() =>
            {
                OutputTextBox.Text += args.Data + '\n';

                //Paragraph errorParagraph = new Paragraph()
                //{
                //    Foreground = Brushes.OrangeRed
                //};
                //errorParagraph.Inlines.Add(new Run(args.Data + '\n'));
                //
                //OutputFlowDocument.Blocks.Add(errorParagraph);

                OutputTextBox.ScrollToEnd();
            });

            // Start the process
            if (File.Exists(ubtProcess.StartInfo.FileName) && ubtProcess.Start())
            {
                ubtProcess.BeginOutputReadLine();
            }

            ubtProcess.Exited += (se, ev) =>
            {
                // Run Unreal Automation Tool

                //                                                          IDK IF I NEED THIS I DONT FULLY REMEMBER WHY IT'S HERE
                if (uatProcess != null && uatProcess.HasExited == false)
                {
                    return;
                }

                uatProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\RunUAT.bat",
                        //Arguments = $"BuildCookRun -Project=\"{uprojectPath}\" -NoP4 -NoCompileEditor -Distribution -TargetPlatform=Win64 -Platform=Win64 -ClientConfig=Shipping -ServerConfig=Shipping -Cook -Build -Stage -Pak -source -Prereqs -Package", // No compile - assuming you're using UBT before this
                        Arguments = $"BuildCookRun -Project=\"{uprojectPath}\" -NoP4 -NoCompileEditor -Distribution -TargetPlatform=Win64 -Platform=Win64 -ClientConfig=Shipping -ServerConfig=Shipping -Cook -Build -Stage -Pak -source -Prereqs -Package -Compile", // Includes the "Compile" commandlet which I think is required for source builds
                        //Arguments = $"BuildCookRun -Project=\"{uprojectPath}\" -NoP4 -Distribution -TargetPlatform=Win64 -Platform=Win64 -ClientConfig=Shipping -ServerConfig=Shipping -Cook -Build -Stage -Pak -Archive -source -Prereqs -Package", // With compile - doesn't work for some reason
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                uatProcess.StartInfo.UseShellExecute = false;
                uatProcess.StartInfo.RedirectStandardOutput = true;
                uatProcess.OutputDataReceived += (s, args) => Dispatcher.Invoke(() =>
                {
                    OutputTextBox.Text += args.Data + '\n';

                    //Paragraph goodParagraph = new Paragraph()
                    //{
                    //    Foreground = Brushes.Black
                    //};
                    //goodParagraph.Inlines.Add(new Run(args.Data + '\n'));
                    //
                    //OutputFlowDocument.Blocks.Add(goodParagraph);

                    OutputTextBox.ScrollToEnd();
                });
                uatProcess.ErrorDataReceived += (s, args) => Dispatcher.Invoke(() =>
                {
                    OutputTextBox.Text += args.Data + '\n';

                    //Paragraph goodParagraph = new Paragraph()
                    //{
                    //    Foreground = Brushes.Black
                    //};
                    //goodParagraph.Inlines.Add(new Run(args.Data + '\n'));
                    //
                    //OutputFlowDocument.Blocks.Add(goodParagraph);

                    OutputTextBox.ScrollToEnd();
                });

                // Start the process
                if (File.Exists(uatProcess.StartInfo.FileName) && uatProcess.Start())
                {
                    uatProcess.BeginOutputReadLine();
                }
            };
        }


        private void OnApplicationEnd(object sender, CancelEventArgs e)
        {
            if (ubtProcess != null)
            {
                ubtProcess.Kill(true);
            }
            if (uatProcess != null)
            {
                uatProcess.Kill(true);
            }
        }
    }
}
