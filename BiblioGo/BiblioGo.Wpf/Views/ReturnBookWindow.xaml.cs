using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Оформити повернення книги»: бібліотекар береться з поточного входу,
    // номер заявки вводиться вручну (його видно в «Мої позики» читача)
    public partial class ReturnBookWindow : Window
    {
        private readonly LoanService _loanService;
        private readonly CurrentUserSession _session;

        public ReturnBookWindow(LoanService loanService, CurrentUserSession session)
        {
            _loanService = loanService;
            _session = session;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                LibrarianTextBlock.Text = "—";
                ReturnBookButton.IsEnabled = false;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            LibrarianTextBlock.Text = $"{user.FirstName} {user.LastName}";
        }

        private async void ReturnBookButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            if (!int.TryParse(LoanIdTextBox.Text, out var loanId))
            {
                ShowError("Введіть числовий номер заявки.");
                return;
            }

            ReturnBookButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Оформлення повернення...";

            try
            {
                var result = await _loanService.ReturnBookAsync(new ReturnBookCommand
                {
                    LoanId = loanId,
                    LibrarianId = user.UserId
                });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                LoanIdTextBox.Clear();
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Повернення за заявкою {loanId} оформлено, примірник знову вільний.";
            }
            finally
            {
                ReturnBookButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
