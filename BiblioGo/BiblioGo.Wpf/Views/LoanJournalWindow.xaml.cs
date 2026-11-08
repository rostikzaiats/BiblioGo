using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Loans.Dtos;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Application.Lookups.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути журнал позик»: бібліотекар береться з поточного входу, філія — з випадайки,
    // фільтри за статусом і датою оформлення, перемикання сторінок. Номери заявок звідси використовують
    // вікна «Підтвердити видачу» та «Оформити повернення»
    public partial class LoanJournalWindow : Window
    {
        private const int PageSize = 10;

        private static readonly StatusOption[] StatusOptions =
        [
            new("Усі статуси", null),
            new("Очікує підтвердження", LoanStatus.Requested),
            new("Видано", LoanStatus.Issued),
            new("Повернено", LoanStatus.Returned)
        ];

        private readonly LoanService _loanService;
        private readonly LookupService _lookupService;
        private readonly CurrentUserSession _session;

        // Фільтри, з якими показано поточну сторінку: «Попередня»/«Наступна» гортають саме цей результат
        private GetLoanJournalQuery _appliedQuery = new();
        private int _totalPages;

        public LoanJournalWindow(LoanService loanService, LookupService lookupService, CurrentUserSession session)
        {
            _loanService = loanService;
            _lookupService = lookupService;
            _session = session;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusComboBox.ItemsSource = StatusOptions;
            StatusComboBox.SelectedIndex = 0;
            ShowButton.IsEnabled = false;

            var user = _session.User;
            if (user is null)
            {
                LibrarianTextBlock.Text = "—";
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            LibrarianTextBlock.Text = $"{user.FirstName} {user.LastName}";
            ShowInfo("Завантаження філій...");

            var librariesResult = await _lookupService.GetLibrariesAsync();
            if (librariesResult.IsFailure)
            {
                ShowError(librariesResult.Error);
                return;
            }

            if (librariesResult.Value.Count == 0)
            {
                ShowError("Філій не знайдено.");
                return;
            }

            LibraryComboBox.ItemsSource = librariesResult.Value;
            LibraryComboBox.SelectedIndex = 0;

            await LoadPageAsync(CreateQueryFromFilters(pageNumber: 1));
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPageAsync(CreateQueryFromFilters(pageNumber: 1));
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPageAsync(WithPage(_appliedQuery, _appliedQuery.PageNumber - 1));
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPageAsync(WithPage(_appliedQuery, _appliedQuery.PageNumber + 1));
        }

        private GetLoanJournalQuery CreateQueryFromFilters(int pageNumber)
        {
            return new GetLoanJournalQuery
            {
                LibrarianId = _session.User?.UserId ?? 0,
                LibraryId = LibraryComboBox.SelectedValue is int libraryId ? libraryId : 0,
                Status = StatusComboBox.SelectedItem is StatusOption option ? option.Value : null,
                DateFrom = DateFromPicker.SelectedDate,
                DateTo = DateToPicker.SelectedDate,
                PageNumber = pageNumber,
                PageSize = PageSize
            };
        }

        private static GetLoanJournalQuery WithPage(GetLoanJournalQuery query, int pageNumber)
        {
            return new GetLoanJournalQuery
            {
                LibrarianId = query.LibrarianId,
                LibraryId = query.LibraryId,
                Status = query.Status,
                DateFrom = query.DateFrom,
                DateTo = query.DateTo,
                PageNumber = pageNumber,
                PageSize = query.PageSize
            };
        }

        private async Task LoadPageAsync(GetLoanJournalQuery query)
        {
            SetControlsEnabled(false);
            ShowInfo("Завантаження...");

            try
            {
                var result = await _loanService.GetLoanJournalAsync(query);

                if (result.IsFailure)
                {
                    // Помилка валідації чи доступу: попередня сторінка лишається на екрані
                    ShowError(result.Error);
                    return;
                }

                var page = result.Value;
                _appliedQuery = query;
                _totalPages = page.TotalPages;

                LoansDataGrid.ItemsSource = page.Loans.Select(ToRow).ToList();
                PageTextBlock.Text = page.TotalCount == 0
                    ? string.Empty
                    : $"Сторінка {page.PageNumber} з {page.TotalPages} · усього позик: {page.TotalCount}";
                ShowInfo(page.TotalCount == 0 ? "Позик за цими умовами не знайдено." : string.Empty);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            ShowButton.IsEnabled = isEnabled && _session.User is not null;
            PreviousPageButton.IsEnabled = isEnabled && _appliedQuery.PageNumber > 1 && _totalPages > 0;
            NextPageButton.IsEnabled = isEnabled && _appliedQuery.PageNumber < _totalPages;
        }

        private void ShowInfo(string message)
        {
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = message;
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.Red;
            StatusTextBlock.Text = message;
        }

        private static LoanJournalRow ToRow(LoanJournalItemDto loan)
        {
            return new LoanJournalRow(
                loan.LoanId,
                loan.ReaderName,
                loan.BookTitle,
                loan.InventoryNumber,
                GetStatusText(loan),
                FormatDate(loan.IssueDate),
                FormatDate(loan.DueDate),
                FormatDate(loan.ReturnDate));
        }

        private static string GetStatusText(LoanJournalItemDto loan)
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

        // Дати в БД зберігаються в UTC, бібліотекарю показуємо локальний час
        private static string FormatDate(DateTime? date)
        {
            return date.HasValue ? date.Value.ToLocalTime().ToString("dd.MM.yyyy") : "—";
        }

        private sealed record StatusOption(string Name, LoanStatus? Value);

        // Рядок таблиці: уже готові до показу значення
        public sealed record LoanJournalRow(
            int LoanId,
            string ReaderName,
            string BookTitle,
            int InventoryNumber,
            string Status,
            string IssueDate,
            string DueDate,
            string ReturnDate);
    }
}
