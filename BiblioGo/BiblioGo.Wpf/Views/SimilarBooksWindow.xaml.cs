using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути схожі книги»: читач обирає книгу і бачить інші книги того ж автора або жанру
    public partial class SimilarBooksWindow : Window
    {
        private static readonly int[] TopOptions = [5, 10, 20];

        private readonly BookCatalogService _bookCatalogService;
        private readonly LookupService _lookupService;

        public SimilarBooksWindow(BookCatalogService bookCatalogService, LookupService lookupService)
        {
            _bookCatalogService = bookCatalogService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TopComboBox.ItemsSource = TopOptions;
            TopComboBox.SelectedItem = 10;

            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження списку книг...");

            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return;
            }

            BookComboBox.ItemsSource = booksResult.Value;
            ShowInfo(booksResult.Value.Count == 0 ? "У каталозі ще немає книг." : string.Empty);
            ShowButton.IsEnabled = true;
        }

        // Обрано іншу книгу: ховаємо схожі на попередню, щоб їх не сплутати
        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BooksDataGrid.ItemsSource = null;
            ShowInfo(string.Empty);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть книгу.»
            var query = new GetSimilarBooksQuery
            {
                BookId = BookComboBox.SelectedValue is int bookId ? bookId : 0,
                Top = TopComboBox.SelectedItem is int top ? top : 0
            };

            ShowButton.IsEnabled = false;
            ShowInfo("Пошук схожих книг...");

            var result = await _bookCatalogService.GetSimilarBooksAsync(query);

            ShowButton.IsEnabled = true;

            if (result.IsFailure)
            {
                BooksDataGrid.ItemsSource = null;
                ShowError(result.Error);
                return;
            }

            BooksDataGrid.ItemsSource = result.Value;
            ShowInfo(result.Value.Count == 0
                ? "Схожих книг у каталозі поки немає."
                : $"Знайдено схожих книг: {result.Value.Count}");
        }

        private void ShowInfo(string message)
        {
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = message;
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
