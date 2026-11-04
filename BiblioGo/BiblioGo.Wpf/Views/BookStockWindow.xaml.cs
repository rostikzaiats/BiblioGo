using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Dtos;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути кількість доступних та виданих книг»: книга та (за потреби) філія з випадайок
    public partial class BookStockWindow : Window
    {
        private const string AllLibrariesName = "Усі філії";

        private readonly BookCopyService _bookCopyService;
        private readonly LookupService _lookupService;

        public BookStockWindow(BookCopyService bookCopyService, LookupService lookupService)
        {
            _bookCopyService = bookCopyService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження списків...");

            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return;
            }

            var librariesResult = await _lookupService.GetLibrariesAsync();
            if (librariesResult.IsFailure)
            {
                ShowError(librariesResult.Error);
                return;
            }

            // Перший пункт з Id = 0 означає «усі філії» (у запит піде LibraryId = null)
            var libraries = new List<LookupItemDto> { new() { Id = 0, Name = AllLibrariesName } };
            libraries.AddRange(librariesResult.Value);

            BookComboBox.ItemsSource = booksResult.Value;
            LibraryComboBox.ItemsSource = libraries;
            LibraryComboBox.SelectedIndex = 0;

            ShowInfo(booksResult.Value.Count == 0 ? "У каталозі ще немає книг." : string.Empty);
            ShowButton.IsEnabled = true;
        }

        // Обрано іншу книгу чи філію: ховаємо залишки попереднього вибору, щоб їх не сплутати з новими
        private void Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            StockPanel.Visibility = Visibility.Collapsed;
            ShowInfo(string.Empty);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть книгу»
            var bookId = BookComboBox.SelectedValue is int selectedBookId ? selectedBookId : 0;
            int? libraryId = LibraryComboBox.SelectedValue is int selectedLibraryId && selectedLibraryId > 0
                ? selectedLibraryId
                : null;
            var scope = (LibraryComboBox.SelectedItem as LookupItemDto)?.Name ?? AllLibrariesName;

            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження...");

            try
            {
                var result = await _bookCopyService.GetStockAsync(new GetBookStockQuery
                {
                    BookId = bookId,
                    LibraryId = libraryId
                });

                if (result.IsFailure)
                {
                    StockPanel.Visibility = Visibility.Collapsed;
                    ShowError(result.Error);
                    return;
                }

                ShowStock(result.Value, scope);
                ShowInfo(string.Empty);
            }
            catch (Exception)
            {
                // Сервіс сам повертає помилки як Result; сюди потрапляє лише непередбачене
                StockPanel.Visibility = Visibility.Collapsed;
                ShowError("Не вдалося завантажити залишки книги. Спробуйте пізніше.");
            }
            finally
            {
                ShowButton.IsEnabled = true;
            }
        }

        private void ShowStock(BookStockDto stock, string scope)
        {
            TitleTextBlock.Text = stock.Title;
            AuthorTextBlock.Text = stock.Author;
            ScopeTextBlock.Text = $"Філія: {scope}";
            TotalTextBlock.Text = stock.TotalCopies.ToString();
            AvailableTextBlock.Text = stock.AvailableCopies.ToString();
            IssuedTextBlock.Text = stock.IssuedCopies.ToString();
            WrittenOffTextBlock.Text = stock.WrittenOffCopies.ToString();

            StockPanel.Visibility = Visibility.Visible;
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
