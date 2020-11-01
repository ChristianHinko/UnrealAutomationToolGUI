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
    enum EngineType
    {
        [Description("Rocket")]
        TYPE_Rocket,

        [Description("Source")]
        TYPE_Source,

        [Description("Installed")]
        TYPE_Installed
    }

    enum BuildConfiguration
    {
        [Description("Debug")]
        BUILD_Debug,

        [Description("DebugGame")]
        BUILD_DebugGame,

        [Description("Development")]
        BUILD_Development,

        [Description("Shipping")]
        BUILD_Shipping,

        [Description("Test")]
        BUILD_Test
    }

    enum TargetPlatform
    {
        [Description("Win32")]
        PLAT_Win32,

        [Description("Win64")]
        PLAT_Win64,

        [Description("HoloLens")]
        PLAT_HoloLens,

        [Description("Mac")]
        PLAT_Mac,

        [Description("XboxOne")]
        PLAT_XboxOne,

        [Description("PS4")]
        PLAT_PS4,

        [Description("IOS")]
        PLAT_IOS,

        [Description("Android")]
        PLAT_Android,

        [Description("HTML5")]
        PLAT_HTML5,

        [Description("Linux")]
        PLAT_Linux,

        [Description("LinuxAArch64")]
        PLAT_LinuxAArch64,

        [Description("AllDesktop")]
        PLAT_AllDesktop,

        [Description("TVOS")]
        PLAT_TVOS,

        [Description("Switch")]
        PLAT_Switch,

        [Description("Lumin")]
        PLAT_Lumin
    }

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

        EngineType engineType { get; set; }

        BuildConfiguration buildConfiguration { get; set; }

        TargetPlatform targetPlatform { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Load user preferences
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

                // Save this preference to user settings
                Settings.Default.EngineDirectory = EngineDirectoryTextBox.Text;
                Settings.Default.Save();
            }
        }
        private void UProjectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select file

            CommonOpenFileDialog uprojectFolderDialog = new CommonOpenFileDialog();
            uprojectFolderDialog.Filters.Add(new CommonFileDialogFilter("uproject file", ".uproject"));


            if (uprojectFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                UProjectPathTextBox.Text = uprojectFolderDialog.FileName;

                // Save this preference to user settings
                Settings.Default.UProjectPath = UProjectPathTextBox.Text;
                Settings.Default.Save();
            }
        }
        private void BuildBtn_Click(object sender, RoutedEventArgs e)
        {
            // The directory that holds the engine
            string engineDirectory = EngineDirectoryTextBox.Text;

            // The path to the .uproject
            string uprojectPath = UProjectPathTextBox.Text;

            // The directory that the uproject is in
            string uprojectDirectory = uprojectPath.Remove(uprojectPath.LastIndexOf('\\'));

            // The name of the uproject (excluding the ".uproject")
            string uprojectName = uprojectPath.Remove(uprojectPath.LastIndexOf(".uproject")).Remove(0, uprojectPath.Remove(uprojectPath.LastIndexOf(".uproject")).LastIndexOf('\\') + 1);

            // The name of this project's editor
            string editorName = uprojectName + "Editor";


            //                                                                          Run Unreal Build Tool

            ubtProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\Build.bat",
                    Arguments = $"{editorName} Win64 Development \"{uprojectPath}\" -WaitMutex",
                    //Arguments = $"\"uprojectPath\" \"{uprojectDirectory}\\Intermediate\\Build\\Win64\\{editorName}\\DebugGame\\{editorName}.uhtmanifest\" -LogCmds=\"loginit warning, logexit warning, logdatabase error\" -Unattended -WarningsAsErrors -abslog=\"{engineDirectory}\\Engine\\Programs\\UnrealBuildTool\\Log_UHT.txt\"", // i forgot what this was from
                    //Arguments = $"-Target=\"{editorName} Win64 DebugGame - Project =\"{uprojectPath}\"\" - Target = \"ShaderCompileWorker Win64 Development -Quiet\" - WaitMutex - FromMsBuild", // What VS runs
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
                // Read the process's output so we can display it in the text box
                ubtProcess.BeginOutputReadLine();
            }

            ubtProcess.Exited += (se, ev) =>
            {
                //                                                                      Run Unreal Automation Tool


                string uatArguments = $"BuildCookRun -Project=\"{uprojectPath}\" -NoP4 -NoCompileEditor -Distribution -Platform=Win64 -Cook -Build -Stage -Pak -Prereqs -Package";
                Dispatcher.Invoke(() =>
                {
                    uatArguments += BuildUATArguments();
                });

                uatProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\RunUAT.bat",
                        Arguments = uatArguments,
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
                    // Read the process's output so we can display it in the text box
                    uatProcess.BeginOutputReadLine();
                }
            };
        }

        string BuildUATArguments()
        {
            string retVal = "";



            string engineTypeArg = "";
            switch ((EngineType)EngineTypeCombo.SelectedItem)
            {
                case EngineType.TYPE_Rocket:
                    engineTypeArg = "Rocket";
                    break;
                case EngineType.TYPE_Source:
                    engineTypeArg = "source";
                    break;
                case EngineType.TYPE_Installed:
                    //engineTypeArg = "source";      //TODO: TEMP idk what arg to use for this
                    break;
            }
            engineTypeArg = " -" + engineTypeArg;
            retVal += engineTypeArg;


            string buildConfigurationArg = "";
            switch ((BuildConfiguration)BuildConfigurationCombo.SelectedItem)
            {
                case BuildConfiguration.BUILD_Debug:
                    buildConfigurationArg = "Debug";
                    break;
                case BuildConfiguration.BUILD_DebugGame:
                    buildConfigurationArg = "DebugGame";
                    break;
                case BuildConfiguration.BUILD_Development:
                    buildConfigurationArg = "Development";
                    break;
                case BuildConfiguration.BUILD_Shipping:
                    buildConfigurationArg = "Shipping";
                    break;
                case BuildConfiguration.BUILD_Test:
                    buildConfigurationArg = "Test";
                    break;
                default:
                    buildConfigurationArg = "Unknown";
                    break;
            }
            buildConfigurationArg = $" -ClientConfig={buildConfigurationArg} -ServerConfig={buildConfigurationArg}";
            retVal += buildConfigurationArg;


            string targetPlatformArg = "";
            switch ((TargetPlatform)TargetPlatformCombo.SelectedItem) // TODO: THESE SHOULD BE CHECK BOXES ACTUALLY NOT COMBO BOX
            {
                case TargetPlatform.PLAT_Win32:
                    targetPlatformArg = "Win32";
                    break;
                case TargetPlatform.PLAT_Win64:
                    targetPlatformArg = "Win64";
                    break;
                case TargetPlatform.PLAT_HoloLens:
                    targetPlatformArg = "HoloLens";
                    break;
                case TargetPlatform.PLAT_Mac:
                    targetPlatformArg = "Mac";
                    break;
                case TargetPlatform.PLAT_XboxOne:
                    targetPlatformArg = "XboxOne";
                    break;
                case TargetPlatform.PLAT_PS4:
                    targetPlatformArg = "PS4";
                    break;
                case TargetPlatform.PLAT_IOS:
                    targetPlatformArg = "IOS";
                    break;
                case TargetPlatform.PLAT_Android:
                    targetPlatformArg = "Android";
                    break;
                case TargetPlatform.PLAT_HTML5:
                    targetPlatformArg = "HTML5";
                    break;
                case TargetPlatform.PLAT_Linux:
                    targetPlatformArg = "Linux";
                    break;
                case TargetPlatform.PLAT_LinuxAArch64:
                    targetPlatformArg = "LinuxAArch64";
                    break;
                case TargetPlatform.PLAT_AllDesktop:
                    targetPlatformArg = "AllDesktop";
                    break;
                case TargetPlatform.PLAT_TVOS:
                    targetPlatformArg = "TVOS";
                    break;
                case TargetPlatform.PLAT_Switch:
                    targetPlatformArg = "Switch";
                    break;
                case TargetPlatform.PLAT_Lumin:
                    targetPlatformArg = "Lumin";
                    break;
                default:
                    break;
            }
            targetPlatformArg = $" -TargetPlatform={targetPlatformArg}";
            retVal += targetPlatformArg;



            return retVal;
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

        private void EngineTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            engineType = (EngineType)EngineTypeCombo.SelectedItem;
        }

        private void BuildConfigurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buildConfiguration = (BuildConfiguration)BuildConfigurationCombo.SelectedItem;
        }

        private void TargetPlatformCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            targetPlatform = (TargetPlatform)TargetPlatformCombo.SelectedItem;
        }
    }
}
