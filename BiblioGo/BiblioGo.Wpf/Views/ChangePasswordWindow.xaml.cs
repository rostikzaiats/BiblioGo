using System.Windows;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;

        public ChangePasswordWindow(UserService userService, CurrentUserSession session)
        {
            _userService = userService;
            _session = session;
            InitializeComponent();
        }

        private async void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            if (user is null)
            {
                StatusText.Text = "Спочатку увійдіть.";
                return;
            }

            if (NewPasswordInput.Password != ConfirmPasswordInput.Password)
            {
                StatusText.Text = "Нові паролі не збігаються";
                return;
            }

            ChangeButton.IsEnabled = false;
            try
            {
                var command = new ChangePasswordCommand
                {
                    UserId = user.UserId,
                    CurrentPassword = CurrentPasswordInput.Password,
                    NewPassword = NewPasswordInput.Password
                };
                var result = await _userService.ChangePasswordAsync(command);
                if (result.IsFailure)
                {
                    StatusText.Text = result.Error;
                    return;
                }

                CurrentPasswordInput.Clear();
                NewPasswordInput.Clear();
                ConfirmPasswordInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = "Готово! Пароль змінено";
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                StatusText.Text = "Не вдалося змінити пароль. Спробуйте пізніше.";
            }
            finally
            {
                ChangeButton.IsEnabled = true;
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
    }
}
