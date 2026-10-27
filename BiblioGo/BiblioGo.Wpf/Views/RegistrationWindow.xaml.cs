using System.Windows;
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
                var result = await _userService.RegisterAsync(command);
                if (result.IsFailure)
                {
                    // Помилка валідації: показуємо її текст користувачу
                    StatusText.Text = result.Error;
                    return;
                }
                PasswordInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Користувача створено, id: {result.Value}";
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
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try {   DialogResult = false; } catch {}
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
