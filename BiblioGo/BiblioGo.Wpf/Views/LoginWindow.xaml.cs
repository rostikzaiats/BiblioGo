using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using BiblioGo.Wpf.Session;
namespace BiblioGo.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;
        private readonly IServiceProvider _serviceProvider;

        public LoginWindow(UserService userService, CurrentUserSession session, IServiceProvider serviceProvider)
        {
            _userService = userService;
            _session = session;
            _serviceProvider = serviceProvider;
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
                _session.User = result.Value;
                DialogResult = true;
            }
            finally
            {
                PasswordInput.Clear();
                LoginButton.IsEnabled = true;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = _serviceProvider.GetRequiredService<RegistrationWindow>();
            registerWindow.Owner = this;
            registerWindow.ShowDialog();
            if(registerWindow.ShowDialog() == true)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = "Реєстрація успішна! Тепер ви можете увійти.";
            }
        }
    }
}
