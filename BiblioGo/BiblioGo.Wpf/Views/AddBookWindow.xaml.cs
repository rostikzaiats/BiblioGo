using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Додати нову книгу до каталогу»: кнопка викликає сервіс, сервіс — репозиторій
    public partial class AddBookWindow : Window
    {
        private readonly BookCopyService _bookCopyService;

        public AddBookWindow(BookCopyService bookCopyService)
        {
            _bookCopyService = bookCopyService;
            InitializeComponent();
        }

        private async void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            // Нечислове значення року — 0: сервіс поверне помилку валідації з допустимим діапазоном
            var year = int.TryParse(YearTextBox.Text.Trim(), out var parsedYear) ? parsedYear : 0;

            AddBookButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Додавання книги...";

            try
            {
                var result = await _bookCopyService.AddBookAsync(new AddBookCommand
                {
                    Title = TitleTextBox.Text,
                    Author = AuthorTextBox.Text,
                    Genre = GenreTextBox.Text,
                    Year = year,
                    Description = DescriptionTextBox.Text
                });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                TitleTextBox.Clear();
                AuthorTextBox.Clear();
                GenreTextBox.Clear();
                YearTextBox.Clear();
                DescriptionTextBox.Clear();
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Книгу додано до каталогу (№ {result.Value}).";
            }
            catch (Exception)
            {
                // Сервіс сам повертає помилки як Result; сюди потрапляє лише непередбачене
                ShowError("Не вдалося додати книгу. Спробуйте пізніше.");
            }
            finally
            {
                AddBookButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
