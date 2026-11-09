using System.Windows;
using BiblioGo.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BiblioGo.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        private void BookCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            var bookCatalogWindow = _serviceProvider.GetRequiredService<BookCatalogWindow>();
            bookCatalogWindow.Owner = this;
            bookCatalogWindow.Show();
        }

        private void PopularBooksButton_Click(object sender, RoutedEventArgs e)
        {
            var popularBooksWindow = _serviceProvider.GetRequiredService<PopularBooksWindow>();
            popularBooksWindow.Owner = this;
            popularBooksWindow.Show();
        }

        private void BookSearchButton_Click(object sender, RoutedEventArgs e)
        {
            var bookSearchWindow = _serviceProvider.GetRequiredService<BookSearchWindow>();
            bookSearchWindow.Owner = this;
            bookSearchWindow.Show();
        }

        private void BookSearchByAuthorButton_Click(object sender, RoutedEventArgs e)
        {
            var bookSearchByAuthorWindow = _serviceProvider.GetRequiredService<BookSearchByAuthorWindow>();
            bookSearchByAuthorWindow.Owner = this;
            bookSearchByAuthorWindow.Show();
        }

        private void BookDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var bookDetailsWindow = _serviceProvider.GetRequiredService<BookDetailsWindow>();
            bookDetailsWindow.Owner = this;
            bookDetailsWindow.Show();
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = _serviceProvider.GetRequiredService<RegistrationWindow>();
            registrationWindow.Owner = this;
            registrationWindow.Show();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Owner = this;
            loginWindow.ShowDialog();
        }
        private void UpdateUserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var updateUserProfileWindow = _serviceProvider.GetRequiredService<UpdateUserProfileWindow>();
            updateUserProfileWindow.Owner = this;
            updateUserProfileWindow.Show();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var changePasswordWindow = _serviceProvider.GetRequiredService<ChangePasswordWindow>();
            changePasswordWindow.Owner = this;
            changePasswordWindow.Show();
        }

        private void ChangeUserRoleButton_Click(object sender, RoutedEventArgs e)
        {
            var changeUserRoleWindow = _serviceProvider.GetRequiredService<ChangeUserRoleWindow>();
            changeUserRoleWindow.Owner = this;
            changeUserRoleWindow.Show();
        }

        private void WriteOffBookCopyButton_Click(object sender, RoutedEventArgs e)
        {
            var writeOffWindow = _serviceProvider.GetRequiredService<BookCopyWriteOffWindow>();
            writeOffWindow.Owner = this;
            writeOffWindow.Show();
        }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            var addBookWindow = _serviceProvider.GetRequiredService<AddBookWindow>();
            addBookWindow.Owner = this;
            addBookWindow.Show();
        }

        private void BookStockButton_Click(object sender, RoutedEventArgs e)
        {
            var bookStockWindow = _serviceProvider.GetRequiredService<BookStockWindow>();
            bookStockWindow.Owner = this;
            bookStockWindow.Show();
        }

        private void AddBookCopiesButton_Click(object sender, RoutedEventArgs e)
        {
            var addBookCopiesWindow = _serviceProvider.GetRequiredService<AddBookCopiesWindow>();
            addBookCopiesWindow.Owner = this;
            addBookCopiesWindow.Show();
        }

        private void CreateLoanRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var loanRequestWindow = _serviceProvider.GetRequiredService<LoanRequestWindow>();
            loanRequestWindow.Owner = this;
            loanRequestWindow.Show();
        }

        private void UserLoansButton_Click(object sender, RoutedEventArgs e)
        {
            var userLoansWindow = _serviceProvider.GetRequiredService<UserLoansWindow>();
            userLoansWindow.Owner = this;
            userLoansWindow.Show();
        }

        private void ConfirmLoanIssueButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmLoanIssueWindow = _serviceProvider.GetRequiredService<ConfirmLoanIssueWindow>();
            confirmLoanIssueWindow.Owner = this;
            confirmLoanIssueWindow.Show();
        }

        private void ReturnBookButton_Click(object sender, RoutedEventArgs e)
        {
            var returnBookWindow = _serviceProvider.GetRequiredService<ReturnBookWindow>();
            returnBookWindow.Owner = this;
            returnBookWindow.Show();
        }

        private void LoanJournalButton_Click(object sender, RoutedEventArgs e)
        {
            var loanJournalWindow = _serviceProvider.GetRequiredService<LoanJournalWindow>();
            loanJournalWindow.Owner = this;
            loanJournalWindow.Show();
        }
    }
}
