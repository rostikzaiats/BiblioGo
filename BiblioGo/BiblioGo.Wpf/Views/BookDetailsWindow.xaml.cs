using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути інформацію про книгу»: обираємо книгу з випадайки, сервіс повертає опис і наявність
    public partial class BookDetailsWindow : Window
    {
        private readonly BookCatalogService _bookCatalogService;
        private readonly LookupService _lookupService;

        public BookDetailsWindow(BookCatalogService bookCatalogService, LookupService lookupService)
        {
            _bookCatalogService = bookCatalogService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть книгу.»
            var bookId = BookComboBox.SelectedValue is int selectedId ? selectedId : 0;

            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження...");

            try
            {
                var result = await _bookCatalogService.GetBookDetailsAsync(bookId);

                if (result.IsFailure)
                {
                    DetailsPanel.Visibility = Visibility.Collapsed;
                    ShowError(result.Error);
                    return;
                }

                ShowDetails(result.Value);
                ShowInfo(string.Empty);
            }
            finally
            {
                ShowButton.IsEnabled = true;
            }
        }

        private void ShowDetails(BookDetailsDto details)
        {
            TitleTextBlock.Text = details.Title;
            AuthorTextBlock.Text = details.Author;
            GenreYearTextBlock.Text = $"Жанр: {details.Genre}    Рік: {details.Year}";
            DescriptionTextBlock.Text = string.IsNullOrWhiteSpace(details.Description) ? "Опису немає." : details.Description;

            AvailabilityTextBlock.Text = details.AvailableCopies == 0
                ? $"Зараз вільних примірників немає (на руках у читачів: {details.IssuedCopies})."
                : $"Вільних примірників: {details.AvailableCopies}, на руках у читачів: {details.IssuedCopies}.";

            LibrariesDataGrid.ItemsSource = details.AvailableInLibraries;
            LibrariesDataGrid.Visibility = details.AvailableInLibraries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            DetailsPanel.Visibility = Visibility.Visible;
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
