using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути популярні книги»: читач обирає період і бачить рейтинг книг за кількістю видач
    public partial class PopularBooksWindow : Window
    {
        private static readonly PeriodOption[] PeriodOptions =
        [
            new("За весь час", PopularBooksPeriod.AllTime),
            new("За останній місяць", PopularBooksPeriod.LastMonth),
            new("За останні 3 місяці", PopularBooksPeriod.LastThreeMonths),
            new("За останній рік", PopularBooksPeriod.LastYear)
        ];

        private static readonly int[] TopOptions = [5, 10, 20, 50];

        private readonly BookCatalogService _bookCatalogService;

        public PopularBooksWindow(BookCatalogService bookCatalogService)
        {
            _bookCatalogService = bookCatalogService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PeriodComboBox.ItemsSource = PeriodOptions;
            PeriodComboBox.SelectedIndex = 0;
            TopComboBox.ItemsSource = TopOptions;
            TopComboBox.SelectedItem = 10;

            await LoadPopularBooksAsync();
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPopularBooksAsync();
        }

        private async Task LoadPopularBooksAsync()
        {
            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження...");

            try
            {
                var query = new GetPopularBooksQuery
                {
                    Period = PeriodComboBox.SelectedValue is PopularBooksPeriod period ? period : PopularBooksPeriod.AllTime,
                    Top = TopComboBox.SelectedItem is int top ? top : 0
                };

                var result = await _bookCatalogService.GetPopularBooksAsync(query);

                if (result.IsFailure)
                {
                    BooksDataGrid.ItemsSource = null;
                    ShowError(result.Error);
                    return;
                }

                BooksDataGrid.ItemsSource = result.Value;
                ShowInfo(result.Value.Count == 0 ? "За цей період книг ще не видавали." : string.Empty);
            }
            catch (Exception)
            {
                // Сервіс сам повертає помилки як Result; сюди потрапляє лише непередбачене
                BooksDataGrid.ItemsSource = null;
                ShowError("Не вдалося завантажити популярні книги. Спробуйте пізніше.");
            }
            finally
            {
                ShowButton.IsEnabled = true;
            }
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

        private sealed record PeriodOption(string Name, PopularBooksPeriod Value);
    }
}
