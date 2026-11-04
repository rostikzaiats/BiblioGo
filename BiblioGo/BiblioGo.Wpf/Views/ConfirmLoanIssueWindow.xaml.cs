using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Підтвердити та оформити видачу книги»: бібліотекар береться з поточного входу,
    // номер заявки вводиться вручну (його видно в «Мої позики» читача), термін повернення за замовчуванням 14 днів
    public partial class ConfirmLoanIssueWindow : Window
    {
        private const int DefaultLoanDays = 14;

        private readonly LoanService _loanService;
        private readonly CurrentUserSession _session;

        public ConfirmLoanIssueWindow(LoanService loanService, CurrentUserSession session)
        {
            _loanService = loanService;
            _session = session;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DueDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            DueDatePicker.SelectedDate = DateTime.Today.AddDays(DefaultLoanDays);

            var user = _session.User;
            if (user is null)
            {
                LibrarianTextBlock.Text = "—";
                ConfirmIssueButton.IsEnabled = false;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            LibrarianTextBlock.Text = $"{user.FirstName} {user.LastName}";
        }

        private async void ConfirmIssueButton_Click(object sender, RoutedEventArgs e)
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

            if (DueDatePicker.SelectedDate is not DateTime dueDate)
            {
                ShowError("Оберіть дату повернення.");
                return;
            }

            ConfirmIssueButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Оформлення видачі...";

            try
            {
                var result = await _loanService.ConfirmIssueAsync(new ConfirmLoanIssueCommand
                {
                    LoanId = loanId,
                    LibrarianId = user.UserId,
                    DueDate = dueDate
                });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                LoanIdTextBox.Clear();
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Видачу за заявкою {loanId} підтверджено. Повернути до {dueDate:dd.MM.yyyy}.";
            }
            finally
            {
                ConfirmIssueButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
