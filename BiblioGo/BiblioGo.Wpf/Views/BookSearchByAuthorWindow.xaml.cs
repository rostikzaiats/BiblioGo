using System.Windows;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Знайти книгу за автором»: кнопка викликає сервіс, сервіс — репозиторій
    public partial class BookSearchByAuthorWindow : Window
    {
        private readonly BookCatalogService _bookCatalogService;

        public BookSearchByAuthorWindow(BookCatalogService bookCatalogService)
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
                var books = await _bookCatalogService.SearchByAuthorAsync(AuthorTextBox.Text);

                BooksDataGrid.ItemsSource = books;
                StatusTextBlock.Text = books.Count == 0
                    ? "Книг цього автора не знайдено."
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
