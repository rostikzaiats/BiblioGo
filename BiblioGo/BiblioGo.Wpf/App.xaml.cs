using System.Windows;
using Serilog;

namespace BiblioGo.Wpf
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "BiblioGo")
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("BiblioGo started on {MachineName}", Environment.MachineName);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("BiblioGo stopped");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}