using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Dtos;
using BiblioGo.Application.Lookups.Services;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути примірники книги»: книга з випадайки, за потреби — філія та статус
    public partial class BookCopiesWindow : Window
    {
        private const string AllLibrariesName = "Усі філії";

        // Відповідає порядку елементів у StatusComboBox: Усі, Доступний, Виданий, Списаний
        private static readonly BookCopyStatus?[] StatusFilters =
        {
            null,
            BookCopyStatus.Available,
            BookCopyStatus.Issued,
            BookCopyStatus.WrittenOff
        };

        private readonly BookCopyService _bookCopyService;
        private readonly LookupService _lookupService;

        public BookCopiesWindow(BookCopyService bookCopyService, LookupService lookupService)
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

        // Змінився вибір: ховаємо таблицю попереднього запиту, щоб її не сплутати з новою
        private void Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Подія приходить і під час InitializeComponent (SelectedIndex у XAML), коли таблиці ще немає
            if (CopiesDataGrid is null)
            {
                return;
            }

            CopiesDataGrid.ItemsSource = null;
            ShowInfo(string.Empty);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть книгу»
            var bookId = BookComboBox.SelectedValue is int selectedBookId ? selectedBookId : 0;
            int? libraryId = LibraryComboBox.SelectedValue is int selectedLibraryId && selectedLibraryId > 0
                ? selectedLibraryId
                : null;
            var status = StatusComboBox.SelectedIndex >= 0 ? StatusFilters[StatusComboBox.SelectedIndex] : null;

            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження...");

            try
            {
                var result = await _bookCopyService.GetCopiesAsync(new GetBookCopiesQuery
                {
                    BookId = bookId,
                    LibraryId = libraryId,
                    Status = status
                });

                if (result.IsFailure)
                {
                    CopiesDataGrid.ItemsSource = null;
                    ShowError(result.Error);
                    return;
                }

                var copies = result.Value;
                CopiesDataGrid.ItemsSource = copies.Select(ToRow).ToList();
                ShowInfo(copies.Count == 0
                    ? "Примірників не знайдено."
                    : $"Знайдено примірників: {copies.Count}");
            }
            finally
            {
                ShowButton.IsEnabled = true;
            }
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

        private static CopyRow ToRow(BookCopyDto copy)
        {
            return new CopyRow(copy.InventoryNumber, copy.LibraryName, GetStatusText(copy.Status));
        }

        private static string GetStatusText(BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Available => "Доступний",
                BookCopyStatus.Issued => "Виданий",
                BookCopyStatus.WrittenOff => "Списаний",
                _ => status.ToString()
            };
        }

        // Рядок таблиці: уже готові до показу значення
        public sealed record CopyRow(int InventoryNumber, string LibraryName, string Status);
    }
}
