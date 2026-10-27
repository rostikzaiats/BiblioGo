using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using System.Windows;
using BiblioGo.Wpf.Session;
namespace BiblioGo.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;

        public LoginWindow(UserService userService, CurrentUserSession session)
        {
            _userService = userService;
            _session = session;
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
                var result = await _userService.LoginAsync(command);

                if (result.IsFailure)
                {
                    // Помилка валідації: показуємо її текст користувачу
                    StatusText.Text = result.Error;
                    return;
                }
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Користувач увійшов, id: {result.Value.UserId}";
                DialogResult = true;
                _session.User = result.Value;
            }
            finally
            {
                PasswordInput.Clear();
                LoginButton.IsEnabled = true;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try { DialogResult = false; } catch { }
            Close();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
