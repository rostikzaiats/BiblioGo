using System.Windows;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;

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
                var books = await _bookCatalogService.SearchByTitleAsync(TitleTextBox.Text);

                BooksDataGrid.ItemsSource = books;
                StatusTextBlock.Text = books.Count == 0
                    ? "Книг із такою назвою не знайдено."
                    : $"Знайдено книг: {books.Count}";
            }
            catch (InputValidationException ex)
            {
                BooksDataGrid.ItemsSource = null;
                StatusTextBlock.Text = ex.Message;
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
