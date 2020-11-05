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
    ///     - Add option to import .ini configuration
    ///     - Fix Light->Dark mode switching
    /// </summary>
    public partial class MainWindow : Window
    {
        Process ubtProcess;
        Process uatProcess;
        bool pendingProcessKill;

        int timesRun = 0;

        Brush logColor = Brushes.Black;
        Brush warningColor = Brushes.Yellow;
        Brush errorColor = Brushes.Red;
        Brush markerColor = Brushes.Aqua;
        Brush goodColor = Brushes.LawnGreen;






        string engineDirectory { get; set; }
        string uProjectPath { get; set; }
        EngineType engineType { get; set; }

        BuildConfiguration buildConfiguration { get; set; }
        TargetPlatform targetPlatform { get; set; }

        bool build { get; set; }
        bool cook { get; set; }
        bool package { get; set; }

        bool pak { get; set; }

        bool stage { get; set; }
        bool skipStage { get; set; }
        string stagingDirectory { get; set; }

        bool server { get; set; }
        bool noClient { get; set; }

        bool archive { get; set; }
        string archiveDirectory { get; set; }

        string createReleaseVersion { get; set; }
        string basedOnReleaseVersion { get; set; }
        bool generatePatch { get; set; }

        string customArgs { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            // Load user preferences

            engineDirectory = Settings.Default.EngineDirectory;
            EngineDirectoryTextBox.Text = engineDirectory;

            uProjectPath = Settings.Default.UProjectPath;
            UProjectPathTextBox.Text = uProjectPath;

            engineType = (EngineType)Settings.Default.EngineTypeIndex;
            EngineTypeCombo.SelectedIndex = (int)engineType;

            DarkThemeCheckBox.IsChecked = Settings.Default.DarkMode;
            DarkOutputCheckBox.IsChecked = Settings.Default.DarkOutput;
        }

        private void EngineDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog engineDirectoryDialog = new CommonOpenFileDialog("Engine directory");
            engineDirectoryDialog.IsFolderPicker = true;


            if (engineDirectoryDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                engineDirectory = engineDirectoryDialog.FileName;
                EngineDirectoryTextBox.Text = engineDirectory;

                // Save this preference to user settings
                Settings.Default.EngineDirectory = engineDirectory;
                Settings.Default.Save();
                Settings.Default.Reload();
            }
        }
        private void UProjectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select file

            CommonOpenFileDialog uprojectFolderDialog = new CommonOpenFileDialog();
            uprojectFolderDialog.Filters.Add(new CommonFileDialogFilter("uproject file", ".uproject"));


            if (uprojectFolderDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                uProjectPath = uprojectFolderDialog.FileName;
                UProjectPathTextBox.Text = uProjectPath;

                // Save this preference to user settings
                Settings.Default.UProjectPath = uProjectPath;
                Settings.Default.Save();
                Settings.Default.Reload();
            }
        }
        private void EngineTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            engineType = (EngineType)EngineTypeCombo.SelectedItem;

            // Save this preference to user settings
            Settings.Default.EngineTypeIndex = (int)engineType;
            Settings.Default.Save();
            Settings.Default.Reload();
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            // Keep track of how many times we've ran
            ++timesRun;

            // We clicked build, so at this point we know the user doesn't want to cancel build
            pendingProcessKill = false;

            // Output this run's header
            Paragraph paragraph = new Paragraph()
            {
                Foreground = Brushes.Magenta
            };
            paragraph.Inlines.Add(new Run($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ RUN {timesRun} ~~~~~~~~~~~~~~~~~~~"));
            OutputFlowDocument.Blocks.Add(paragraph);
            OutputRichTextBox.ScrollToEnd();



            // The directory that the uproject is in
            string uprojectDirectory = uProjectPath.Remove(uProjectPath.LastIndexOf('\\'));

            // The name of the uproject (excluding the ".uproject")
            string uprojectName = uProjectPath.Remove(uProjectPath.LastIndexOf(".uproject")).Remove(0, uProjectPath.Remove(uProjectPath.LastIndexOf(".uproject")).LastIndexOf('\\') + 1);

            // The name of this project's editor
            string editorName = uprojectName + "Editor";


            // Run Unreal Build Tool

            ubtProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = $"{engineDirectory}\\Engine\\Build\\BatchFiles\\Build.bat",
                    Arguments = $"{editorName} Win64 Development \"{uProjectPath}\" -WaitMutex",
                    //Arguments = $"\"uprojectPath\" \"{uprojectDirectory}\\Intermediate\\Build\\Win64\\{editorName}\\DebugGame\\{editorName}.uhtmanifest\" -LogCmds=\"loginit warning, logexit warning, logdatabase error\" -Unattended -WarningsAsErrors -abslog=\"{engineDirectory}\\Engine\\Programs\\UnrealBuildTool\\Log_UHT.txt\"", // i forgot what this was from
                    //Arguments = $"-Target=\"{editorName} Win64 DebugGame - Project =\"{uprojectPath}\"\" - Target = \"ShaderCompileWorker Win64 Development -Quiet\" - WaitMutex - FromMsBuild", // What VS runs
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            ubtProcess.StartInfo.UseShellExecute = false;
            ubtProcess.StartInfo.RedirectStandardOutput = true;
            ubtProcess.OutputDataReceived += (sender, e) => Dispatcher.Invoke(() =>
            {
                string output = e.Data;
                if (output != null)
                {
                    Paragraph paragraph = new Paragraph();

                    if (output.Contains("ERROR: ", StringComparison.OrdinalIgnoreCase) || output.Contains("FAILED"))
                    {
                        paragraph.Foreground = errorColor;
                    }
                    else if (output.Contains("WARNING: ", StringComparison.OrdinalIgnoreCase))
                    {
                        paragraph.Foreground = warningColor;
                    }
                    else if (output.Contains("**********"))
                    {
                        paragraph.Foreground = markerColor;
                    }
                    else if (output.Contains("SUCCESSFUL"))
                    {
                        paragraph.Foreground = goodColor;
                    }
                    else
                    {
                        paragraph.Foreground = logColor;
                    }

                    // Print the output
                    paragraph.Inlines.Add(new Run(output));
                    OutputFlowDocument.Blocks.Add(paragraph);

                    OutputRichTextBox.ScrollToEnd();
                }
            });
            ubtProcess.ErrorDataReceived += (sender, e) => Dispatcher.Invoke(() =>
            {
                string output = e.Data;
                if (output != null)
                {
                    Paragraph paragraph = new Paragraph();

                    paragraph.Foreground = errorColor;

                    // Print the output
                    paragraph.Inlines.Add(new Run(output));
                    OutputFlowDocument.Blocks.Add(paragraph);

                    OutputRichTextBox.ScrollToEnd();
                }
            });

            // Ensure the user wants us to start this process
            if (pendingProcessKill)
            {
                return;
            }

            // Start the process
            if (File.Exists(ubtProcess.StartInfo.FileName) && ubtProcess.Start())
            {
                Dispatcher.Invoke(() =>
                {
                    Paragraph paragraph = new Paragraph()
                    {
                        Foreground = Brushes.Magenta
                    };
                    paragraph.Inlines.Add(new Run("UBT Started"));
                    OutputFlowDocument.Blocks.Add(paragraph);
                    OutputRichTextBox.ScrollToEnd();
                });

                // Read the process's output so we can display it in the text box
                ubtProcess.BeginOutputReadLine();
            }

            ubtProcess.Exited += (sender, e) =>
            {
                ubtProcess.Dispose();
                ubtProcess = null;

                Dispatcher.Invoke(() =>
                {
                    Paragraph paragraph = new Paragraph()
                    {
                        Foreground = Brushes.Magenta
                    };
                    paragraph.Inlines.Add(new Run("UBT Exited"));
                    OutputFlowDocument.Blocks.Add(paragraph);
                    OutputRichTextBox.ScrollToEnd();
                });


                // Ensure the user wants us to start this process
                if (pendingProcessKill)
                {
                    return;
                }


                // Run Unreal Automation Tool

                string uatArguments = $"BuildCookRun -Platform=Win64 -Project=\"{uProjectPath}\" -NoCompileEditor -NoP4";
                uatArguments += BuildUATArguments();

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
                uatProcess.OutputDataReceived += (sender, e) => Dispatcher.Invoke(() =>
                {
                    string output = e.Data;
                    if (output != null)
                    {
                        Paragraph paragraph = new Paragraph();

                        if (output.Contains("ERROR: ", StringComparison.OrdinalIgnoreCase) || output.Contains("FAILED"))
                        {
                            paragraph.Foreground = errorColor;
                        }
                        else if (output.Contains("WARNING: ", StringComparison.OrdinalIgnoreCase))
                        {
                            paragraph.Foreground = warningColor;
                        }
                        else if (output.Contains("**********"))
                        {
                            paragraph.Foreground = markerColor;
                        }
                        else if (output.Contains("SUCCESSFUL"))
                        {
                            paragraph.Foreground = goodColor;
                        }
                        else
                        {
                            paragraph.Foreground = logColor;
                        }

                        // Print the output
                        paragraph.Inlines.Add(new Run(output));
                        OutputFlowDocument.Blocks.Add(paragraph);

                        OutputRichTextBox.ScrollToEnd();
                    }
                });
                uatProcess.ErrorDataReceived += (sender, e) => Dispatcher.Invoke(() =>
                {
                    string output = e.Data;
                    if (output != null)
                    {
                        Paragraph paragraph = new Paragraph();

                        paragraph.Foreground = errorColor;

                        // Print the output
                        paragraph.Inlines.Add(new Run(output));
                        OutputFlowDocument.Blocks.Add(paragraph);

                        OutputRichTextBox.ScrollToEnd();
                    }
                });

                // Start the process
                if (File.Exists(uatProcess.StartInfo.FileName) && uatProcess.Start())
                {
                    Dispatcher.Invoke(() =>
                    {
                        Paragraph paragraph = new Paragraph()
                        {
                            Foreground = Brushes.Magenta
                        };
                        paragraph.Inlines.Add(new Run("UAT Started"));
                        OutputFlowDocument.Blocks.Add(paragraph);
                        OutputRichTextBox.ScrollToEnd();
                    });

                    // Read the process's output so we can display it in the text box
                    uatProcess.BeginOutputReadLine();
                }

                uatProcess.Exited += (sender, e) =>
                {
                    uatProcess.Dispose();
                    uatProcess = null;

                    Dispatcher.Invoke(() =>
                    {
                        Paragraph paragraph = new Paragraph()
                        {
                            Foreground = Brushes.Magenta
                        };
                        paragraph.Inlines.Add(new Run("UAT Exited"));
                        OutputFlowDocument.Blocks.Add(paragraph);
                        OutputRichTextBox.ScrollToEnd();
                    });
                };
            };
        }

        string BuildUATArguments()
        {
            string retVal = "";


            // -<Engine Type>
            string engineTypeArg = "";
            switch (engineType)
            {
                case EngineType.TYPE_Rocket:
                    engineTypeArg = " -Rocket";
                    break;
                case EngineType.TYPE_Source:
                    engineTypeArg = " -Source -Compile";
                    break;
                case EngineType.TYPE_Installed:
                    //engineTypeArg = " -Source -Compile";      //TODO: TEMP idk what arg to use for this
                    break;
            }
            retVal += engineTypeArg;


            // -ClientConfig=<Configuration> -ServerConfig=<Configuration>
            string buildConfigurationArg = "";
            switch (buildConfiguration)
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


            // -TargetPlatform=<Platform1>+<Platform2>
            string targetPlatformArg = "";
            switch (targetPlatform) // TODO: THESE SHOULD BE CHECK BOXES ACTUALLY NOT COMBO BOX
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


            // -Build
            string buildArg = "";
            if (build)
            {
                buildArg = " -Build";
            }
            retVal += buildArg;


            // -Cook
            string cookArg = "";
            if (cook)
            {
                // requires -Stage or -SkipStage
                cookArg = " -Cook";
            }
            retVal += cookArg;


            // -Package
            string packageArg = "";
            if (package)
            {
                packageArg = " -Package -Distribution -Prereqs";
            }
            retVal += packageArg;


            // -Pak
            string pakArg = "";
            if (pak)
            {
                // requires -Stage or -SkipStage
                pakArg = " -Pak";
            }
            retVal += pakArg;


            // -Stage
            string stageArg = "";
            if (stage)
            {
                stageArg = " -Stage";
            }
            retVal += stageArg;


            // -SkipStage
            string skipStageArg = "";
            if (skipStage)
            {
                skipStageArg = " -SkipStage";
            }
            retVal += skipStageArg;


            // StagingDirectory=<Dir>
            string stagingDirectoryArg = "";
            if (stagingDirectory != null && stagingDirectory.Length > 0)
            {
                stagingDirectoryArg = $" -StagingDirectory=\"{stagingDirectory}\"";
            }
            retVal += stagingDirectoryArg;


            // -Server
            string serverArg = "";
            if (server)
            {
                serverArg = " -Server";
            }
            retVal += serverArg;


            // -NoClient
            string noClientArg = "";
            if (noClient)
            {
                noClientArg = " -NoClient";
            }
            retVal += noClientArg;


            // -Archive
            string archiveArg = "";
            if (archive)
            {
                archiveArg = " -Archive";
            }
            retVal += archiveArg;


            // ArchiveDirectory=<Dir>
            string archiveDirectoryArg = "";
            if (archiveDirectory != null && archiveDirectory.Length > 0)
            {
                archiveDirectoryArg = $" -ArchiveDirectory=\"{archiveDirectory}\"";
            }
            retVal += archiveDirectoryArg;


            string createReleaseVersionArg = "";
            if (createReleaseVersion != null && createReleaseVersion.Length > 0)
            {
                createReleaseVersionArg = $" -CreateReleaseVersion=\"{createReleaseVersion}\"";
            }
            retVal += createReleaseVersionArg;

            string generatePatchArg = "";
            if (generatePatch)
            {
                // Requires -BasedOnReleaseVersion
                generatePatchArg = " -GeneratePatch";
            }
            retVal += generatePatchArg;

            string basedOnReleaseVersionArg = "";
            if (basedOnReleaseVersion != null && basedOnReleaseVersion.Length > 0)
            {
                basedOnReleaseVersionArg = $" -BasedOnReleaseVersion=\"{basedOnReleaseVersion}\"";
            }
            retVal += basedOnReleaseVersionArg;








            ///// <summary>
            ///// Cook: Only cook maps (and referenced content) instead of cooking everything only affects cookall flag
            ///// </summary>
            //[Help("CookAll", "Cook all the things in the content directory for this project")]
            //public bool CookAll;






            if (customArgs != null && customArgs.Length > 0)
            {
                retVal += ' ' + customArgs;
            }
           


            return retVal;
        }

        private void BuildConfigurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buildConfiguration = (BuildConfiguration)BuildConfigurationCombo.SelectedItem;
        }

        private void TargetPlatformCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            targetPlatform = (TargetPlatform)TargetPlatformCombo.SelectedItem;
        }

        private void BuildCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            build = true;
        }
        private void BuildCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            build = false;
        }

        private void CookCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            cook = true;
        }
        private void CookCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            cook = false;
        }

        private void PackageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            package = true;
        }
        private void PackageCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            package = false;
        }

        private void PakCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            pak = true;
        }
        private void PakCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            pak = false;
        }

        private void StageRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            stage = true;
        }
        private void StageRadioBtn_UnChecked(object sender, RoutedEventArgs e)
        {
            stage = false;
        }
        private void SkipStageRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            skipStage = true;
        }
        private void SkipStageRadioBtn_UnChecked(object sender, RoutedEventArgs e)
        {
            skipStage = false;
        }

        private void ResetStage(object sender, RoutedEventArgs e)
        {
            // Reset this option

            stage = false;
            skipStage = false;
            StageRadioBtn.IsChecked = false;
            SkipStageRadioBtn.IsChecked = false;
        }

        private void StagingDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog stagingDirectoryDialog = new CommonOpenFileDialog("Staging directory");
            stagingDirectoryDialog.IsFolderPicker = true;


            if (stagingDirectoryDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                string dir = stagingDirectoryDialog.FileName;

                StagingDirectoryTextBox.Text = dir;
                stagingDirectory = dir;
            }
        }
        private void ResetStagingDirectory(object sender, RoutedEventArgs e)
        {
            // Reset this option

            stagingDirectory = null;
            StagingDirectoryTextBox.Text = "Default";
        }

        private void ServerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            server = true;
        }
        private void ServerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            server = false;
        }

        private void NoClientCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            noClient = true;
        }
        private void NoClientCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            noClient = false;
        }

        private void ArchiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            archive = true;
        }
        private void ArchiveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            archive = false;
        }

        private void ArchiveDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select folder

            CommonOpenFileDialog archiveDirectoryDialog = new CommonOpenFileDialog("Archive directory");
            archiveDirectoryDialog.IsFolderPicker = true;


            if (archiveDirectoryDialog.ShowDialog().Equals(CommonFileDialogResult.Ok))
            {
                string dir = archiveDirectoryDialog.FileName;

                ArchiveDirectoryTextBox.Text = dir;
                archiveDirectory = dir;
            }
        }
        private void ResetArchiveDirectory(object sender, RoutedEventArgs e)
        {
            // Reset this option

            archiveDirectory = null;
            ArchiveDirectoryTextBox.Text = "Default";
        }

        private void CreateReleaseVersionTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            createReleaseVersion = CreateReleaseVersionTextBox.Text;
        }

        private void BasedOnReleaseVersionTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            basedOnReleaseVersion = BasedOnReleaseVersionTextBox.Text;
        }

        private void GeneratePatchCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            generatePatch = GeneratePatchCheckBox.IsEnabled;
        }
        private void GeneratePatchCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            generatePatch = GeneratePatchCheckBox.IsEnabled;
        }

        private void CustomArgsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            customArgs = CustomArgsTextBox.Text;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            pendingProcessKill = true;
            if (KillProcesses())
            {
                Paragraph paragraph = new Paragraph()
                {
                    Foreground = Brushes.Magenta
                };
                paragraph.Inlines.Add(new Run("KILLED ALL PROCESSES"));
                OutputFlowDocument.Blocks.Add(paragraph);
                OutputRichTextBox.ScrollToEnd();
            }
        }


        private void SettingsTextBlock_MouseEnter(object sender, RoutedEventArgs e)
        {
            SettingsRect.Visibility = Visibility.Visible;
            SettingsMouseCoverage.Visibility = Visibility.Visible;
            DarkThemeCheckBox.Visibility = Visibility.Visible;
            DarkOutputCheckBox.Visibility = Visibility.Visible;
            RightClickTip.Visibility = Visibility.Visible;
        }
        private void SettingsTextBlock_MouseLeave(object sender, RoutedEventArgs e)
        {
            SettingsRect.Visibility = Visibility.Hidden;
            SettingsMouseCoverage.Visibility = Visibility.Hidden;
            DarkThemeCheckBox.Visibility = Visibility.Hidden;
            DarkOutputCheckBox.Visibility = Visibility.Hidden;
            RightClickTip.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// This dark mode is really broken but I don't care the only point of it is to make the output easier
        /// to read. Switching between light and dark more than once breaks things and switching from dark to light requires
        /// an app restart.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DarkThemeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.DarkMode = true;
            Settings.Default.Save();
            Settings.Default.Reload();


            Brush primaryColor = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Brush secondaryColor = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            Brush tertiaryColor = new SolidColorBrush(Color.FromRgb(62, 62, 64));

            Application.Current.MainWindow.Background = secondaryColor;
            Application.Current.MainWindow.BorderBrush = primaryColor;


            Brush newLogColor = Brushes.LightGray;

            OutputRichTextBox.Background = primaryColor;
            OutputRichTextBox.BorderBrush = secondaryColor;
            foreach (Block block in OutputFlowDocument.Blocks)
            {
                if (block.Foreground == logColor)
                {
                    block.Foreground = newLogColor;
                }
            }
            logColor = newLogColor;



            SettingsRect.Fill = primaryColor;
            SettingsRect.Stroke = secondaryColor;

            foreach (TextBox textBox in Grid.Children.OfType<TextBox>())
            {
                //textBox.Style = (Style)FindResource(ToolBar.TextBoxStyleKey);

                //textBox.Background = tertiaryColor;
                //textBox.BorderBrush = secondaryColor;
                textBox.Foreground = logColor;
                if (textBox == CustomArgsTextBox)
                {
                    textBox.Background = tertiaryColor;
                    textBox.BorderBrush = secondaryColor;
                }
            }
            foreach (TextBlock textBlock in Grid.Children.OfType<TextBlock>())
            {
                //textBlock.Background = tertiaryColor;
                //textBlock.BorderBrush = secondaryColor;
                textBlock.Foreground = logColor;
            }
            foreach (Label label in Grid.Children.OfType<Label>())
            {
                if (label == MessageOnProcesses)
                {
                    continue;
                }

                label.Foreground = logColor;
            }
            foreach (CheckBox checkBox in Grid.Children.OfType<CheckBox>())
            {
                //checkBox.Style = (Style)FindResource(ToolBar.CheckBoxStyleKey);

                checkBox.Background = tertiaryColor;
                checkBox.BorderBrush = secondaryColor;
                checkBox.Foreground = logColor;
            }
            foreach (RadioButton radioButton in Grid.Children.OfType<RadioButton>())
            {
                //radioButton.Style = (Style)FindResource(ToolBar.RadioButtonStyleKey);

                radioButton.Background = tertiaryColor;
                radioButton.BorderBrush = secondaryColor;
                radioButton.Foreground = logColor;
            }
            foreach (Button button in Grid.Children.OfType<Button>())
            {
                button.Style = (Style)FindResource(ToolBar.ButtonStyleKey);

                button.Background = tertiaryColor;
                button.BorderBrush = secondaryColor;
                button.Foreground = logColor;

                button.Resources["ComboBoxItem.ItemsviewSelected.Border"] = tertiaryColor;

                if (button == CancelBtn)
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(124, 62, 64));
                }
            }
            foreach (ComboBox comboBox in Grid.Children.OfType<ComboBox>())
            {
                comboBox.Style = (Style)FindResource(ToolBar.ComboBoxStyleKey);

                comboBox.Background = secondaryColor;
                comboBox.BorderBrush = tertiaryColor;
                comboBox.Foreground = logColor;

                comboBox.Resources["ComboBoxItem.ItemsviewSelected.Border"] = tertiaryColor;
            }
        }
        /// <summary>
        /// Going from dark to light theme requires an app restart because this function doesn't even undo everything the
        /// dark theme function did.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DarkThemeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.DarkMode = false;
            Settings.Default.Save();
            Settings.Default.Reload();


            if (DarkOutputCheckBox.IsChecked == false)
            {
                Brush newLogColor = Brushes.Black;

                foreach (Block block in OutputFlowDocument.Blocks)
                {
                    if (block.Foreground == logColor)
                    {
                        block.Foreground = newLogColor;
                    }
                }
                logColor = newLogColor;
                OutputRichTextBox.Background = Brushes.White;
            }


            Application.Current.MainWindow.Background = Brushes.White;
            SettingsRect.Fill = Brushes.White;

            foreach (CheckBox checkBox in Grid.Children.OfType<CheckBox>())
            {
                checkBox.Background = Brushes.White;
                checkBox.Foreground = logColor;
            }
            foreach (RadioButton radioButton in Grid.Children.OfType<RadioButton>())
            {
                radioButton.Background = Brushes.White;
                radioButton.Foreground = logColor;
            }
            foreach (Button button in Grid.Children.OfType<Button>())
            {
                button.Background = Brushes.White;
                button.Foreground = logColor;
            }
            foreach (ComboBox comboBox in Grid.Children.OfType<ComboBox>())
            {
                comboBox.Background = Brushes.White;
                comboBox.Foreground = logColor;
            }
        }
        private void DarkOutputCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.DarkOutput = true;
            Settings.Default.Save();
            Settings.Default.Reload();


            Brush newLogColor = Brushes.LightGray;

            if (DarkThemeCheckBox.IsChecked == true)
            {
                OutputRichTextBox.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                OutputRichTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            }
            else
            {
                OutputRichTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                OutputRichTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }
            //OutputRichTextBox.SelectionBrush = new SolidColorBrush(Color.FromArgb(150, 0, 200, 255));
            foreach (Block block in OutputFlowDocument.Blocks)
            {
                if (block.Foreground == logColor)
                {
                    block.Foreground = newLogColor;
                }
            }
            logColor = newLogColor;
        }
        private void DarkOutputCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.DarkOutput = false;
            Settings.Default.Save();
            Settings.Default.Reload();

            if (DarkThemeCheckBox.IsChecked == false)
            {
                Brush newLogColor = Brushes.Black;

                OutputRichTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                OutputRichTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
                foreach (Block block in OutputFlowDocument.Blocks)
                {
                    if (block.Foreground == logColor)
                    {
                        block.Foreground = newLogColor;
                    }
                }
                logColor = newLogColor;
            }
        }


        private void Window_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.DragMove();
        }


        private void OnApplicationEnd(object sender, CancelEventArgs e)
        {
            KillProcesses();
        }

        private bool KillProcesses()
        {
            bool killedAProcess = false;

            if (ubtProcess != null)
            {
                ubtProcess.Kill(true);
                ubtProcess = null; // Ensure null

                killedAProcess = true;
            }
            if (uatProcess != null)
            {
                uatProcess.Kill(true);
                uatProcess = null; // Ensure null

                killedAProcess = true;
            }

            return killedAProcess;
        }
    }
}
