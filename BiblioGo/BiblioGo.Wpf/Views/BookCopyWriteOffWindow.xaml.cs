using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Lookups.Dtos;
using BiblioGo.Application.Lookups.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Списати примірник»: спочатку обираємо книгу, потім один із її вільних примірників
    public partial class BookCopyWriteOffWindow : Window
    {
        private readonly BookCopyService _bookCopyService;
        private readonly LookupService _lookupService;

        public BookCopyWriteOffWindow(BookCopyService bookCopyService, LookupService lookupService)
        {
            _bookCopyService = bookCopyService;
            _lookupService = lookupService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteOffButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Завантаження списку книг...";

            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return;
            }

            BookComboBox.ItemsSource = booksResult.Value;
            StatusTextBlock.Text = string.Empty;
            WriteOffButton.IsEnabled = true;
        }

        // Книга змінилась: підвантажуємо вільні примірники саме цієї книги
        private async void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CopyComboBox.ItemsSource = null;

            if (BookComboBox.SelectedValue is not int bookId)
            {
                return;
            }

            var result = await _lookupService.GetAvailableCopiesAsync(bookId);

            // Поки йшло завантаження, користувач міг обрати іншу книгу — старий результат ігноруємо
            if (BookComboBox.SelectedValue is not int currentBookId || currentBookId != bookId)
            {
                return;
            }

            if (result.IsFailure)
            {
                ShowError(result.Error);
                return;
            }

            CopyComboBox.ItemsSource = result.Value;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = result.Value.Count == 0
                ? "Для цієї книги немає вільних примірників для списання."
                : string.Empty;
        }

        private async void WriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (CopyComboBox.SelectedValue is not int bookCopyId || CopyComboBox.SelectedItem is not LookupItemDto copy)
            {
                ShowError("Оберіть примірник.");
                return;
            }

            var confirmation = MessageBox.Show(
                this,
                $"Списати примірник «{copy.Name}»? Цю дію не можна скасувати.",
                "Підтвердження списання",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            WriteOffButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Списання...";

            try
            {
                await _bookCopyService.WriteOffAsync(new WriteOffBookCopyCommand { BookCopyId = bookCopyId });

                // Списаний примірник більше не вільний: прибираємо його зі списку без повторного запиту до БД
                CopyComboBox.ItemsSource = CopyComboBox.Items
                    .OfType<LookupItemDto>()
                    .Where(item => item.Id != bookCopyId)
                    .ToList();

                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Примірник «{copy.Name}» списано.";
            }
            catch (BiblioGoException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                ShowError("Не вдалося списати примірник. Спробуйте пізніше.");
            }
            finally
            {
                WriteOffButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
