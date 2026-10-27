using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Wpf.Session;
using System.Windows;

namespace BiblioGo.Wpf.Views
{
    /// <summary>
    /// Interaction logic for UpdateUserProfileWindow.xaml
    /// </summary>
    public partial class UpdateUserProfileWindow : Window
    {
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;

        public UpdateUserProfileWindow(UserService userService, CurrentUserSession session)
        {
            _userService = userService;
            _session = session;
            InitializeComponent();
            if (_session.User != null)
            {
                FirstNameInput.Text = _session.User.FirstName;
                LastNameInput.Text = _session.User.LastName;
                PhoneInput.Text = _session.User.Phone;
            }
        }
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null) { StatusText.Text = "Спочатку увійдіть."; return; }
            UpdateButton.IsEnabled = false;
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            try
            {
                var command = new UpdateUserCommand {
                    UserId = user.UserId,
                    FirstName = FirstNameInput.Text,
                    LastName = LastNameInput.Text,
                    Phone = PhoneInput.Text
                };
                var result = await _userService.UpdateUserAsync(command);
                if (result.IsFailure)
                {
                    // Помилка валідації: показуємо її текст користувачу
                    StatusText.Text = result.Error;
                    return ;
                }
                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.Phone = command.Phone;
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = "Готово! Профіль оновлено";
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісо
                StatusText.Text = "Не вдалося виконати оновлення. Спробуйте пізніше.";
            }
            finally
            {
                UpdateButton.IsEnabled = true;
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
