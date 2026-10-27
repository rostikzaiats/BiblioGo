using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Application.Lookups.Services;
using BiblioGo.Wpf.Session;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Оформити заявку на отримання книги»: читач береться з поточного входу,
    // книга і філія обираються з випадайок
    public partial class LoanRequestWindow : Window
    {
        private readonly LoanService _loanService;
        private readonly LookupService _lookupService;
        private readonly CurrentUserSession _session;

        public LoanRequestWindow(LoanService loanService, LookupService lookupService, CurrentUserSession session)
        {
            _loanService = loanService;
            _lookupService = lookupService;
            _session = session;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateLoanRequestButton.IsEnabled = false;

            var user = _session.User;
            if (user is null)
            {
                ReaderTextBlock.Text = "—";
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            ReaderTextBlock.Text = $"{user.FirstName} {user.LastName}";
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Завантаження списків...";

            var booksResult = await _lookupService.GetBooksAsync();
            if (booksResult.IsFailure)
            {
                ShowError(booksResult.Error);
                return;
            }

            var librariesResult = await _lookupService.GetLibrariesAsync();
            if (librariesResult.IsFailure)
            {
                ShowError(librariesResult.Error);
                return;
            }

            BookComboBox.ItemsSource = booksResult.Value;
            LibraryComboBox.ItemsSource = librariesResult.Value;
            StatusTextBlock.Text = string.Empty;
            CreateLoanRequestButton.IsEnabled = true;
        }

        private async void CreateLoanRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var user = _session.User;
            if (user is null)
            {
                ShowError("Спочатку увійдіть у систему.");
                return;
            }

            if (BookComboBox.SelectedValue is not int bookId)
            {
                ShowError("Оберіть книгу.");
                return;
            }

            if (LibraryComboBox.SelectedValue is not int libraryId)
            {
                ShowError("Оберіть філію бібліотеки.");
                return;
            }

            CreateLoanRequestButton.IsEnabled = false;
            StatusTextBlock.Foreground = Brushes.Black;
            StatusTextBlock.Text = "Оформлення заявки...";

            try
            {
                var loanId = await _loanService.CreateRequestAsync(new CreateLoanRequestCommand
                {
                    UserId = user.UserId,
                    BookId = bookId,
                    LibraryId = libraryId
                });

                BookComboBox.SelectedIndex = -1;
                LibraryComboBox.SelectedIndex = -1;
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
