using System.Windows;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;

namespace BiblioGo.Wpf.Views
{
    public partial class RegistrationWindow : Window
    {
        private readonly UserService _userService;

        public RegistrationWindow(UserService userService)
        {
            _userService = userService;
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

                var id = await _userService.RegisterAsync(command);
                PasswordInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Користувача створено, id: {id}";
            }
            catch (BiblioGoException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісо
                StatusText.Text = "Не вдалося виконати реєстрацію. Спробуйте пізніше.";
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }
    }
}
