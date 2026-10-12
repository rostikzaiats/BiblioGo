using System.Windows;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Handlers;
using BiblioGo.Infrastructure.Persistence;
using BiblioGo.Infrastructure.Repositories;
using BiblioGo.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BiblioGo.Wpf
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterButton.IsEnabled = false;
            StatusText.Foreground = System.Windows.Media.Brushes.Red;

            try
            {
                var command = new RegisterUserCommand
                {
                    Email = EmailInput.Text,
                    Password = PasswordInput.Password,
                    FirstName = FirstNameInput.Text,
                    LastName = LastNameInput.Text,
                    Phone = PhoneInput.Text
                };

                using var db = new ApplicationDbContext(App.DbOptions);
                var repo = new UserRepository(db);
                var hasher = new BcryptPasswordHasher();
                var logger = App.AppLoggerFactory.CreateLogger<RegisterUserHandler>();

                var handler = new RegisterUserHandler(repo, hasher, logger);
                var id = await handler.Handle(command);

                PasswordInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Користувача створено, id: {id}";
            }
            catch (BiblioGoException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during user registration");
                StatusText.Text = "Сталася неочікувана помилка. Деталі є в логах.";
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }
    }
}
