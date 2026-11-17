using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Lookups.Dtos;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Видалити книгу з каталогу»: книга обирається з випадайки, перед видаленням — підтвердження
    public partial class DeleteBookWindow : Window
    {
        private readonly BookCopyService _bookCopyService;
        private readonly LookupService _lookupService;

        public DeleteBookWindow(BookCopyService bookCopyService, LookupService lookupService)
        {
            _bookCopyService = bookCopyService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeleteButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Завантаження списку книг...";

            if (await LoadBooksAsync())
            {
                StatusTextBlock.Text = BookComboBox.Items.Count == 0 ? "У каталозі ще немає книг." : string.Empty;
                DeleteButton.IsEnabled = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookComboBox.SelectedValue is not int bookId || BookComboBox.SelectedItem is not LookupItemDto book)
            {
                ShowError("Оберіть книгу.");
                return;
            }

            var confirmation = MessageBox.Show(
                this,
                $"Видалити книгу «{book.Name}» разом з усіма її примірниками? Цю дію не можна скасувати.",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            DeleteButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Видалення...";

            try
            {
                var result = await _bookCopyService.DeleteBookAsync(new DeleteBookCommand { BookId = bookId });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                // Видалена книга зникає зі списку (а разом з нею й з каталогу)
                if (await LoadBooksAsync())
                {
                    StatusTextBlock.Foreground = Brushes.Green;
                    StatusTextBlock.Text = $"Книгу «{book.Name}» видалено з каталогу.";
                }
            }
            finally
            {
                DeleteButton.IsEnabled = true;
            }
        }

        private async Task<bool> LoadBooksAsync()
        {
            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return false;
            }

            BookComboBox.ItemsSource = booksResult.Value;
            return true;
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
