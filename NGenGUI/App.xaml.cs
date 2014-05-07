using System;
using System.Windows;
using Hiale.NgenGui.Helper;

namespace Hiale.NgenGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0)
            {
                try
                {
                    var parser = new CmdLineParser {ThrowInvalidOptionsException = false};

                    var pathArgument = parser.AddStringParameter("/path", "Directory", false);
                    pathArgument.AddAlias("pATH");
                    pathArgument.AddAlias("-path");
                    pathArgument.AddAlias("-dir");

                    var subDirs = parser.AddBoolSwitch("s", "Including subdirs");

                    var noWindow = parser.AddBoolSwitch("nowindow", "No window is shown");

                    parser.AddHelpOption();

                    //var boolArgument = parser.AddBoolSwitch("-TestBool", "", true);

                    parser.Parse(e.Args);

                    System.Diagnostics.Debug.WriteLine(pathArgument.Value);


                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    
                    //throw;
                }
                


            }
            else
            {
                new MainWindow().ShowDialog();
            }
            Shutdown();
        }
    }
}