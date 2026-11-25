using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути книги, доступні в бібліотеці»: читач обирає філію і бачить,
    // які книги там можна взяти зараз і скільки вільних примірників
    public partial class LibraryBooksWindow : Window
    {
        private readonly BookCatalogService _bookCatalogService;
        private readonly LookupService _lookupService;

        public LibraryBooksWindow(BookCatalogService bookCatalogService, LookupService lookupService)
        {
            _bookCatalogService = bookCatalogService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження списку бібліотек...");

            var librariesResult = await _lookupService.GetLibrariesAsync();
            if (librariesResult.IsFailure)
            {
                ShowError(librariesResult.Error);
                return;
            }

            LibraryComboBox.ItemsSource = librariesResult.Value;
            ShowInfo(librariesResult.Value.Count == 0 ? "У мережі ще немає бібліотек." : string.Empty);
            ShowButton.IsEnabled = true;
        }

        // Обрано іншу бібліотеку: ховаємо книги попередньої, щоб їх не сплутати
        private void LibraryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BooksDataGrid.ItemsSource = null;
            ShowInfo(string.Empty);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть бібліотеку.»
            var query = new GetLibraryBooksQuery
            {
                LibraryId = LibraryComboBox.SelectedValue is int libraryId ? libraryId : 0
            };

            ShowButton.IsEnabled = false;
            ShowInfo("Пошук книг...");

            var result = await _bookCatalogService.GetAvailableBooksInLibraryAsync(query);

            ShowButton.IsEnabled = true;

            if (result.IsFailure)
            {
                BooksDataGrid.ItemsSource = null;
                ShowError(result.Error);
                return;
            }

            BooksDataGrid.ItemsSource = result.Value;
            ShowInfo(result.Value.Count == 0
                ? "Зараз у цій бібліотеці немає вільних книг."
                : $"Книг, які можна взяти: {result.Value.Count}, вільних примірників: {result.Value.Sum(book => book.AvailableCopies)}");
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
