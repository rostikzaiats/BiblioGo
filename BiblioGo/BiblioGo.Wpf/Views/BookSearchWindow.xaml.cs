using System.Windows;
using BiblioGo.Application.Books.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Знайти книгу за назвою»: кнопка викликає сервіс, сервіс — репозиторій
    public partial class BookSearchWindow : Window
    {
        private readonly BookCatalogService _bookCatalogService;

        public BookSearchWindow(BookCatalogService bookCatalogService)
        {
            _bookCatalogService = bookCatalogService;
            InitializeComponent();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchButton.IsEnabled = false;
            StatusTextBlock.Text = "Пошук...";

            try
            {
                var result = await _bookCatalogService.SearchByTitleAsync(TitleTextBox.Text);

                if (result.IsFailure)
                {
                    // Помилка валідації: показуємо її текст користувачу
                    BooksDataGrid.ItemsSource = null;
                    StatusTextBlock.Text = result.Error;
                    return;
                }

                var books = result.Value;
                BooksDataGrid.ItemsSource = books;
                StatusTextBlock.Text = books.Count == 0
                    ? "Книг із такою назвою не знайдено."
                    : $"Знайдено книг: {books.Count}";
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                BooksDataGrid.ItemsSource = null;
                StatusTextBlock.Text = "Не вдалося виконати пошук. Спробуйте пізніше.";
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }
    }
}
