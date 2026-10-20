using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Списати примірник»: кнопка викликає сервіс, сервіс — репозиторій
    public partial class BookCopyWriteOffWindow : Window
    {
        private readonly BookCopyService _bookCopyService;

        public BookCopyWriteOffWindow(BookCopyService bookCopyService)
        {
            _bookCopyService = bookCopyService;
            InitializeComponent();
        }

        private async void WriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(BookCopyIdTextBox.Text, out var bookCopyId))
            {
                ShowError("Введіть числовий ідентифікатор примірника.");
                return;
            }

            var confirmation = MessageBox.Show(
                this,
                $"Списати примірник {bookCopyId}? Цю дію не можна скасувати.",
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

                BookCopyIdTextBox.Clear();
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Примірник {bookCopyId} списано.";
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
