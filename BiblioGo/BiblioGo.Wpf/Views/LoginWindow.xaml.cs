using System.Windows;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;

namespace BiblioGo.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private readonly UserService _userService;

        public LoginWindow(UserService userService)
        {
            _userService = userService;
            InitializeComponent();
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            StatusText.Foreground = System.Windows.Media.Brushes.Red;

            try
            {
                var command = new LoginUserCommand
                {
                    Email = EmailInput.Text,
                    Password = PasswordInput.Password
                };
                var user = await _userService.LoginAsync(command);
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Користувач увійшов, id: {user.UserId}";
            }
            catch (BiblioGoException ex)
            {
                StatusText.Text = ex.Message;
            }
            catch (Exception)
            {
                StatusText.Text = "Не вдалося виконати вхід. Спробуйте пізніше.";
            }
            finally
            {
                PasswordInput.Clear();
                LoginButton.IsEnabled = true;
            }
        }
    }
}
