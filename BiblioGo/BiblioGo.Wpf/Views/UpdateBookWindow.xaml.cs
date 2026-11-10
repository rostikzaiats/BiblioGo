using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Редагувати інформацію про книгу»: обираємо книгу з випадайки, поля заповнюються її даними
    public partial class UpdateBookWindow : Window
    {
        private readonly BookCopyService _bookCopyService;
        private readonly BookCatalogService _bookCatalogService;
        private readonly LookupService _lookupService;

        // Під час оновлення списку книг випадайка скидає вибір; поля при цьому не перезаповнюємо
        private bool _isReloadingBooks;

        public UpdateBookWindow(
            BookCopyService bookCopyService,
            BookCatalogService bookCatalogService,
            LookupService lookupService)
        {
            _bookCopyService = bookCopyService;
            _bookCatalogService = bookCatalogService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowInfo("Завантаження списку книг...");

            if (await LoadBooksAsync(null))
            {
                ShowInfo(BookComboBox.Items.Count == 0 ? "У каталозі ще немає книг." : string.Empty);
            }
        }

        // Обрано книгу: підтягуємо її поточні дані в поля, щоб користувач змінював лише потрібне
        private async void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReloadingBooks)
            {
                return;
            }

            ClearFields();
            SaveButton.IsEnabled = false;

            if (BookComboBox.SelectedValue is not int bookId)
            {
                ShowInfo(string.Empty);
                return;
            }

            ShowInfo("Завантаження даних книги...");

            var result = await _bookCatalogService.GetBookDetailsAsync(bookId);

            // Поки йшло завантаження, користувач міг обрати іншу книгу
            if (BookComboBox.SelectedValue is not int currentBookId || currentBookId != bookId)
            {
                return;
            }

            if (result.IsFailure)
            {
                ShowError(result.Error);
                return;
            }

            FillFields(result.Value);
            ShowInfo(string.Empty);
            SaveButton.IsEnabled = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Нічого не обрано — 0, сервіс поверне помилку валідації «Оберіть книгу»
            var bookId = BookComboBox.SelectedValue is int selectedBookId ? selectedBookId : 0;

            // Нечислове значення року — 0: сервіс поверне помилку валідації з допустимим діапазоном
            var year = int.TryParse(YearTextBox.Text.Trim(), out var parsedYear) ? parsedYear : 0;

            SaveButton.IsEnabled = false;
            ShowInfo("Збереження...");

            try
            {
                var result = await _bookCopyService.UpdateBookAsync(new UpdateBookCommand
                {
                    BookId = bookId,
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

                // Назва чи автор могли змінитися — оновлюємо підписи у випадайці
                if (await LoadBooksAsync(bookId))
                {
                    ShowSuccess("Дані книги оновлено.");
                }
            }
            catch (Exception)
            {
                // Сервіс сам повертає помилки як Result; сюди потрапляє лише непередбачене
                ShowError("Не вдалося зберегти зміни. Спробуйте пізніше.");
            }
            finally
            {
                SaveButton.IsEnabled = BookComboBox.SelectedValue is int;
            }
        }

        // Завантажує книги у випадайку; reselectBookId — книга, яку потрібно обрати знову (після збереження змін)
        private async Task<bool> LoadBooksAsync(int? reselectBookId)
        {
            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return false;
            }

            _isReloadingBooks = true;
            try
            {
                BookComboBox.ItemsSource = booksResult.Value;

                if (reselectBookId is { } bookId)
                {
                    BookComboBox.SelectedValue = bookId;
                }
            }
            finally
            {
                _isReloadingBooks = false;
            }

            return true;
        }

        private void FillFields(BookDetailsDto book)
        {
            TitleTextBox.Text = book.Title;
            AuthorTextBox.Text = book.Author;
            GenreTextBox.Text = book.Genre;
            YearTextBox.Text = book.Year.ToString();
            DescriptionTextBox.Text = book.Description;
        }

        private void ClearFields()
        {
            TitleTextBox.Clear();
            AuthorTextBox.Clear();
            GenreTextBox.Clear();
            YearTextBox.Clear();
            DescriptionTextBox.Clear();
        }

        private void ShowInfo(string message)
        {
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = message;
        }

        private void ShowSuccess(string message)
        {
            StatusTextBlock.Foreground = Brushes.Green;
            StatusTextBlock.Text = message;
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
