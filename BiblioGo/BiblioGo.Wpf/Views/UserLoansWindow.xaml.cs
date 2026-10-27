using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Loans.Dtos;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути власні позики та терміни»: читач береться з поточного входу
    public partial class UserLoansWindow : Window
    {
        // Відповідає порядку елементів у StatusComboBox: Усі, Очікує підтвердження, Видано, Повернено
        private static readonly LoanStatus?[] StatusFilters =
        {
            null,
            LoanStatus.Requested,
            LoanStatus.Issued,
            LoanStatus.Returned
        };

        private readonly LoanService _loanService;
        private readonly CurrentUserSession _session;

        public UserLoansWindow(LoanService loanService, CurrentUserSession session)
        {
            _loanService = loanService;
            _session = session;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                ReaderTextBlock.Text = "—";
                ShowLoansButton.IsEnabled = false;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            ReaderTextBlock.Text = $"{user.FirstName} {user.LastName}";
            await LoadLoansAsync(user.UserId);
        }

        private async void ShowLoansButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                LoansDataGrid.ItemsSource = null;
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            await LoadLoansAsync(user.UserId);
        }

        private async Task LoadLoansAsync(int userId)
        {
            ShowLoansButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Завантаження...";

            try
            {
                var loans = await _loanService.GetUserLoansAsync(new GetUserLoansQuery
                {
                    UserId = userId,
                    Status = StatusFilters[StatusComboBox.SelectedIndex]
                });

                LoansDataGrid.ItemsSource = loans.Select(ToRow).ToList();
                StatusTextBlock.Text = loans.Count == 0
                    ? "Позик не знайдено."
                    : $"Знайдено позик: {loans.Count}";
            }
            catch (BiblioGoException ex)
            {
                LoansDataGrid.ItemsSource = null;
                ShowError(ex.Message);
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                LoansDataGrid.ItemsSource = null;
                ShowError("Не вдалося завантажити позики. Спробуйте пізніше.");
            }
            finally
            {
                ShowLoansButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }

        private static LoanRow ToRow(LoanDto loan)
        {
            return new LoanRow(
                loan.BookTitle,
                loan.BookAuthor,
                loan.LibraryName,
                loan.InventoryNumber,
                GetStatusText(loan),
                FormatDate(loan.IssueDate),
                FormatDate(loan.DueDate),
                FormatDate(loan.ReturnDate));
        }

        private static string GetStatusText(LoanDto loan)
        {
            if (loan.IsOverdue)
            {
                return "Прострочено";
            }

            return loan.Status switch
            {
                LoanStatus.Requested => "Очікує підтвердження",
                LoanStatus.Issued => "Видано",
                LoanStatus.Returned => "Повернено",
                _ => loan.Status.ToString()
            };
        }

        // Дати в БД зберігаються в UTC, читачу показуємо локальний час
        private static string FormatDate(DateTime? date)
        {
            return date.HasValue ? date.Value.ToLocalTime().ToString("dd.MM.yyyy") : "—";
        }

        // Рядок таблиці: уже готові до показу значення
        public sealed record LoanRow(
            string BookTitle,
            string BookAuthor,
            string LibraryName,
            int InventoryNumber,
            string Status,
            string IssueDate,
            string DueDate,
            string ReturnDate);
    }
}
