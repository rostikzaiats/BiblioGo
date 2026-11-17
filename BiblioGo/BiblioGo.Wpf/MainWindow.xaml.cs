using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Enums;
using BiblioGo.Wpf.Session;
using BiblioGo.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace BiblioGo.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
                private readonly IServiceProvider _serviceProvider;
        private readonly UserService _userService;
        private readonly CurrentUserSession _session;

        public MainWindow(IServiceProvider serviceProvider, UserService userService, CurrentUserSession session)
        {
            _serviceProvider = serviceProvider;
            _userService = userService;
            _session = session;
            InitializeComponent();
            ShowSection(CatalogSection, "Каталог");
            ShowCurrentUser();
            ApplyRole(_session.User?.Role ?? UserRole.Reader); // ролі нема -> дефолт користувач
            ProfileMenu.AddHandler(Button.ClickEvent, new RoutedEventHandler((_, _) => ProfilePopup.IsOpen = false));
        }

        private void ShowCurrentUser()
        {
            var user = _session.User;
            if (user is null)
            {
                return;
            }

            ChangeUserRoleButton.Visibility = user.Role == UserRole.Administrator ? Visibility.Visible : Visibility.Collapsed;
            UserNameText.Text = $"{user.FirstName} {user.LastName}";
            AvatarText.Text = user.FirstName.Length > 0 ? user.FirstName[..1].ToUpper() : "?";
        }
        private void ApplyRole(UserRole role)
        {
            var isStaff = role is UserRole.Librarian or UserRole.Administrator;
            var isAdmin = role == UserRole.Administrator;

            // тільки персонал
            FundSectionButton.Visibility = ToVisibility(isStaff);
            ConfirmLoanIssueButton.Visibility = ToVisibility(isStaff);
            ReturnBookButton.Visibility = ToVisibility(isStaff);
            LoanJournalButton.Visibility = ToVisibility(isStaff);

            // тільки адмін
            ChangeUserRoleButton.Visibility = ToVisibility(isAdmin);
        }

        private static Visibility ToVisibility(bool visible)
        {
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }
        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            SectionsPanel.Visibility = SectionsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfilePopup.HorizontalOffset = ProfileButton.ActualWidth - ProfileMenu.Width - 8;
            ProfilePopup.IsOpen = !ProfilePopup.IsOpen;
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProfilePopup.IsOpen && !ProfileButton.IsMouseOver && !ProfileMenu.IsMouseOver)
            {
                ProfilePopup.IsOpen = false;
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            ProfilePopup.IsOpen = false;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_session.User is null)
            {
                return;
            }

            var result = await _userService.LogoutAsync(new LogoutUserCommand { UserId = _session.User.UserId });
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Вихід з акаунта", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _session.User = null;
            ((App)App.Current).Logout(this);
        }
        private void CatalogSectionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(CatalogSection, "Каталог");
        }

        private void FundSectionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(FundSection, "Адміністрування фонду");
        }

        private void LoanSectionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSection(LoanSection, "Видача та повернення книг");
        }

        private void ShowSection(StackPanel section, string title)
        {
            CatalogSection.Visibility = Visibility.Collapsed;
            FundSection.Visibility = Visibility.Collapsed;
            LoanSection.Visibility = Visibility.Collapsed;

            section.Visibility = Visibility.Visible;
            SectionTitleText.Text = title;
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

        private void UpdateBookButton_Click(object sender, RoutedEventArgs e)
        {
            var updateBookWindow = _serviceProvider.GetRequiredService<UpdateBookWindow>();
            updateBookWindow.Owner = this;
            updateBookWindow.Show();
        }

        private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteBookWindow = _serviceProvider.GetRequiredService<DeleteBookWindow>();
            deleteBookWindow.Owner = this;
            deleteBookWindow.Show();
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
