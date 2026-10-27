using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Lookups.Services;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Додати фізичні примірники книги»: книга і філія обираються з випадайок
    public partial class AddBookCopiesWindow : Window
    {
        private readonly BookCopyService _bookCopyService;
        private readonly LookupService _lookupService;

        public AddBookCopiesWindow(BookCopyService bookCopyService, LookupService lookupService)
        {
            _bookCopyService = bookCopyService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddCopiesButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Завантаження списків...";

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

            BookComboBox.ItemsSource = booksResult.Value;
            LibraryComboBox.ItemsSource = librariesResult.Value;
            StatusTextBlock.Text = string.Empty;
            AddCopiesButton.IsEnabled = true;
        }

        private async void AddCopiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookComboBox.SelectedValue is not int bookId)
            {
                ShowError("Оберіть книгу.");
                return;
            }

            if (LibraryComboBox.SelectedValue is not int libraryId)
            {
                ShowError("Оберіть філію бібліотеки.");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out var quantity))
            {
                ShowError("Введіть числову кількість примірників.");
                return;
            }

            AddCopiesButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Додавання примірників...";

            try
            {
                var copies = await _bookCopyService.AddCopiesAsync(new AddBookCopiesCommand
                {
                    BookId = bookId,
                    LibraryId = libraryId,
                    Quantity = quantity
                });

                BookComboBox.SelectedIndex = -1;
                LibraryComboBox.SelectedIndex = -1;
                QuantityTextBox.Text = "1";
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = DescribeAddedCopies(copies);
            }
            catch (BiblioGoException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                ShowError("Не вдалося додати примірники. Спробуйте пізніше.");
            }
            finally
            {
                AddCopiesButton.IsEnabled = true;
            }
        }

        private static string DescribeAddedCopies(IReadOnlyList<BookCopy> copies)
        {
            var first = copies.Min(copy => copy.InventoryNumber);
            var last = copies.Max(copy => copy.InventoryNumber);

            return copies.Count == 1
                ? $"Додано примірник. Інвентарний номер: {first}."
                : $"Додано примірників: {copies.Count}. Інвентарні номери: {first}–{last}.";
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
