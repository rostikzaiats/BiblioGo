using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Оформити заявку на отримання книги»: кнопка викликає сервіс, сервіс — репозиторії
    public partial class LoanRequestWindow : Window
    {
        private readonly LoanService _loanService;

        public LoanRequestWindow(LoanService loanService)
        {
            _loanService = loanService;
            InitializeComponent();
        }

        private async void CreateLoanRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(UserIdTextBox.Text, out var userId))
            {
                ShowError("Введіть числовий ідентифікатор читача.");
                return;
            }

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

            CreateLoanRequestButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Оформлення заявки...";

            try
            {
                var loanId = await _loanService.CreateRequestAsync(new CreateLoanRequestCommand
                {
                    UserId = userId,
                    BookId = bookId,
                    LibraryId = libraryId
                });

                BookIdTextBox.Clear();
                LibraryIdTextBox.Clear();
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Заявку оформлено, очікує підтвердження бібліотекаря. Номер заявки: {loanId}";
            }
            catch (BiblioGoException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                ShowError("Не вдалося оформити заявку. Спробуйте пізніше.");
            }
            finally
            {
                CreateLoanRequestButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
