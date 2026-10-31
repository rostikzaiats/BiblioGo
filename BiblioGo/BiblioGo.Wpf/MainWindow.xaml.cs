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

        private void WriteOffBookCopyButton_Click(object sender, RoutedEventArgs e)
        {
            var writeOffWindow = _serviceProvider.GetRequiredService<BookCopyWriteOffWindow>();
            writeOffWindow.Owner = this;
            writeOffWindow.Show();
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
    }
}
