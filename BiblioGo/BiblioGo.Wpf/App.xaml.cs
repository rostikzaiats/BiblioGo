using System.IO;
using System.Windows;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BiblioGo.Wpf
{
    public partial class App : System.Windows.Application
    {
        public static DbContextOptions<ApplicationDbContext> DbOptions { get; private set; } = null!;
        public static ILoggerFactory AppLoggerFactory { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            DbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "BiblioGo")
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.Console()
                .CreateLogger();

            AppLoggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger, dispose: true));

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