using System.Windows;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Додати нову філію бібліотеки» (Адміністратор): кнопка викликає сервіс, сервіс — репозиторій
    public partial class AddLibraryWindow : Window
    {
        private readonly LibraryService _libraryService;
        private readonly ICurrentUserSession _session;

        public AddLibraryWindow(LibraryService libraryService, ICurrentUserSession session)
        {
            _libraryService = libraryService;
            _session = session;
            InitializeComponent();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            if (user is null)
            {
                StatusText.Text = "Спочатку увійдіть.";
                return;
            }

            AddButton.IsEnabled = false;
            try
            {
                var result = await _libraryService.AddLibraryAsync(new AddLibraryCommand
                {
                    ActorId = user.UserId,
                    Name = NameInput.Text,
                    Address = AddressInput.Text,
                    ContactInfo = ContactInfoInput.Text
                });

                if (result.IsFailure)
                {
                    StatusText.Text = result.Error;
                    return;
                }

                NameInput.Clear();
                AddressInput.Clear();
                ContactInfoInput.Clear();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Готово! Філію додано (№ {result.Value})";
            }
            finally
            {
                AddButton.IsEnabled = true;
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
