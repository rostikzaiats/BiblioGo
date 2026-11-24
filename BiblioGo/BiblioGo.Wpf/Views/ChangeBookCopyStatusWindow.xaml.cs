using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Dtos;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Змінити статус примірника»: бібліотекар береться з поточного входу, інвентарний номер
    // вводиться вручну (його видно в журналі позик і при перевірці наявності), новий статус — з випадайки.
    // «Видано» у випадайці немає: цей статус виставляє лише підтвердження видачі
    public partial class ChangeBookCopyStatusWindow : Window
    {
        private static readonly StatusOption[] StatusOptions =
        [
            new("Доступний", BookCopyStatus.Available),
            new("Списаний", BookCopyStatus.WrittenOff)
        ];

        private readonly LoanService _loanService;
        private readonly CurrentUserSession _session;

        public ChangeBookCopyStatusWindow(LoanService loanService, CurrentUserSession session)
        {
            _loanService = loanService;
            _session = session;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusComboBox.ItemsSource = StatusOptions;
            StatusComboBox.SelectedIndex = 0;

            var user = _session.User;
            if (user is null)
            {
                LibrarianTextBlock.Text = "—";
                ChangeStatusButton.IsEnabled = false;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            LibrarianTextBlock.Text = $"{user.FirstName} {user.LastName}";
        }

        private async void ChangeStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            if (!int.TryParse(InventoryNumberTextBox.Text, out var inventoryNumber))
            {
                ShowError("Введіть числовий інвентарний номер примірника.");
                return;
            }

            if (StatusComboBox.SelectedItem is not StatusOption option)
            {
                ShowError("Оберіть новий статус примірника.");
                return;
            }

            ChangeStatusButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Зміна статусу примірника...";

            try
            {
                var result = await _loanService.ChangeBookCopyStatusAsync(new ChangeBookCopyStatusCommand
                {
                    InventoryNumber = inventoryNumber,
                    NewStatus = option.Value,
                    LibrarianId = user.UserId
                });

                if (result.IsFailure)
                {
                    ShowError(result.Error);
                    return;
                }

                ShowChange(result.Value);
            }
            finally
            {
                ChangeStatusButton.IsEnabled = true;
            }
        }

        private void ShowChange(BookCopyStatusChangeDto change)
        {
            InventoryNumberTextBox.Clear();
            StatusTextBlock.Foreground = Brushes.Green;
            StatusTextBlock.Text =
                $"Примірник №{change.InventoryNumber} («{change.BookTitle}», {change.LibraryName}): " +
                $"статус змінено з «{ToStatusText(change.PreviousStatus)}» на «{ToStatusText(change.NewStatus)}».";
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

        private sealed record StatusOption(string Name, BookCopyStatus Value);
    }
}
