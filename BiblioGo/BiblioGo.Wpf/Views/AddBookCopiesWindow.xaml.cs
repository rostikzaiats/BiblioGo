using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Додати фізичні примірники книги»: кнопка викликає сервіс, сервіс — репозиторій
    public partial class AddBookCopiesWindow : Window
    {
        private readonly BookCopyService _bookCopyService;

        public AddBookCopiesWindow(BookCopyService bookCopyService)
        {
            _bookCopyService = bookCopyService;
            InitializeComponent();
        }

        private async void AddCopiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(BookIdTextBox.Text, out var bookId))
            {
                ShowError("Введіть числовий ідентифікатор книги.");
                return;
            }

            if (!int.TryParse(LibraryIdTextBox.Text, out var libraryId))
            {
                ShowError("Введіть числовий ідентифікатор філії.");
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

                BookIdTextBox.Clear();
                LibraryIdTextBox.Clear();
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
