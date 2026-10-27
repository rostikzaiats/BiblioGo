using System.IO;
using System.Windows;
using BiblioGo.Application;
using BiblioGo.Infrastructure;
using BiblioGo.Wpf.Views;
using BiblioGo.Wpf.Session;
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

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _serviceProvider = ConfigureServices();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                System.Windows.Application.Current.MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("BiblioGo stopped");
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }

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
            services.AddTransient<BookSearchByAuthorWindow>();
            services.AddTransient<RegistrationWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<BookCopyWriteOffWindow>();
            services.AddTransient<AddBookCopiesWindow>();
            services.AddTransient<LoanRequestWindow>();
            services.AddTransient<UserLoansWindow>();
            services.AddSingleton<CurrentUserSession>();
            services.AddTransient<UpdateUserProfileWindow>();

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }
    }
}