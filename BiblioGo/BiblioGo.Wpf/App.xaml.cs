using Serilog;
using System.Configuration;
using System.Data;
using System.Windows;

namespace BiblioGo.Wpf
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.Console()
                .CreateLogger();
            Log.Information("BiblioGo started at {Time}", DateTime.Now);
            base.OnStartup(e);
        }
    }

}
