using System.IO;
using System.Windows;
using BiblioGo.Application;
using BiblioGo.Infrastructure;
using BiblioGo.Wpf.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BiblioGo.Wpf
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "BiblioGo")
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "bibliogo-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("BiblioGo started on {MachineName}", Environment.MachineName);
            base.OnStartup(e);

            _serviceProvider = ConfigureServices();
            _serviceProvider.GetRequiredService<MainWindow>().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("BiblioGo stopped");
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        // Композиційний корінь: реєстрація шарів Application, Infrastructure та вікон
        private static ServiceProvider ConfigureServices()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .Build();

            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddSerilog(dispose: false));
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddTransient<MainWindow>();
            services.AddTransient<BookSearchWindow>();
            services.AddTransient<RegistrationWindow>();
            services.AddTransient<BookCopyWriteOffWindow>();
            services.AddTransient<AddBookCopiesWindow>();
            services.AddTransient<LoanRequestWindow>();
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }
    }
}
