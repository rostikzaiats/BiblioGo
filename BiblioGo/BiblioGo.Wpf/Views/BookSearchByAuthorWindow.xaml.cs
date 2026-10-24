using System.Windows;
using BiblioGo.Application.Books.Services;

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
                var result = await _bookCatalogService.SearchByAuthorAsync(AuthorTextBox.Text);

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
                    ? "Книг цього автора не знайдено."
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
