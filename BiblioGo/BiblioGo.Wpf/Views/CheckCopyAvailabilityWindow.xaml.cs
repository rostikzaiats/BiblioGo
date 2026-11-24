using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Loans.Dtos;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Application.Interfaces;
namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Перевірити наявність примірника перед видачею»: бібліотекар береться з поточного входу,
    // номер заявки вводиться вручну (як у «Підтвердити та оформити видачу книги»); вікно показує примірник за заявкою
    // і чи він вільний на полиці
    public partial class CheckCopyAvailabilityWindow : Window
    {
        private readonly LoanService _loanService;
        private readonly ICurrentUserSession _session;

        public CheckCopyAvailabilityWindow(LoanService loanService, ICurrentUserSession session)
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
                CheckButton.IsEnabled = false;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            LibrarianTextBlock.Text = $"{user.FirstName} {user.LastName}";
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
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

            CheckButton.IsEnabled = false;
            DetailsPanel.Visibility = Visibility.Collapsed;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Перевірка примірника...";

            try
            {
                var result = await _loanService.CheckCopyAvailabilityAsync(new CheckCopyAvailabilityQuery
                {
                    LoanId = loanId,
                    LibrarianId = user.UserId
                });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                ShowAvailability(result.Value);
            }
            finally
            {
                CheckButton.IsEnabled = true;
            }
        }

        private void ShowAvailability(CopyAvailabilityDto availability)
        {
            BookTextBlock.Text = $"Книга: {availability.BookTitle} — {availability.BookAuthor}";
            LibraryTextBlock.Text = $"Філія: {availability.LibraryName}";
            InventoryNumberTextBlock.Text = $"Інвентарний номер: {availability.InventoryNumber}";
            CopyStatusTextBlock.Text = $"Статус примірника: {ToStatusText(availability.CopyStatus)}";
            DetailsPanel.Visibility = Visibility.Visible;

            if (availability.IsAvailable)
            {
                StatusTextBlock.Foreground = Brushes.Green;
                StatusTextBlock.Text = $"Примірник за заявкою {availability.LoanId} є на полиці — книгу можна видавати.";
                return;
            }

            ShowError($"Видати за заявкою {availability.LoanId} не можна. {availability.UnavailableReason}.");
        }

        private static string ToStatusText(BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Available => "доступний",
                BookCopyStatus.Issued => "виданий",
                BookCopyStatus.WrittenOff => "списаний",
                _ => status.ToString()
            };
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }
    }
}
