using System.Windows;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    public partial class ChangeUserRoleWindow : Window
    {
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;

        public ChangeUserRoleWindow(UserService userService, CurrentUserSession session)
        {
            _userService = userService;
            _session = session;
            InitializeComponent();

            RoleComboBox.ItemsSource = new[]
            {
                new RoleOption(UserRole.Reader, "Читач"),
                new RoleOption(UserRole.Librarian, "Бібліотекар"),
                new RoleOption(UserRole.Administrator, "Адміністратор")
            };
            RoleComboBox.SelectedIndex = 0;
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

            if (RoleComboBox.SelectedValue is not UserRole newRole)
            {
                StatusText.Text = "Оберіть роль зі списку";
                return;
            }

            ChangeButton.IsEnabled = false;
            try
            {
                var command = new ChangeUserRoleCommand
                {
                    ActorId = user.UserId,
                    TargetEmail = EmailInput.Text,
                    NewRole = newRole
                };
                var result = await _userService.ChangeUserRoleAsync(command);
                if (result.IsFailure)
                {
                    StatusText.Text = result.Error;
                    return;
                }

                EmailInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = "Готово! Роль змінено";
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                StatusText.Text = "Не вдалося змінити роль. Спробуйте пізніше.";
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

        private sealed record RoleOption(UserRole Role, string Name);
    }
}