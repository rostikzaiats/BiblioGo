using System.Windows;
using System.Windows.Media;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Переглянути каталог книг»: фільтри за жанром і роком, сортування, перемикання сторінок
    public partial class BookCatalogWindow : Window
    {
        private const int PageSize = 10;
        private const string AllGenres = "Усі жанри";

        private static readonly SortOption[] SortOptions =
        [
            new("За назвою", CatalogSortBy.Title),
            new("За автором", CatalogSortBy.Author),
            new("Спочатку новіші", CatalogSortBy.YearNewest),
            new("Спочатку старіші", CatalogSortBy.YearOldest)
        ];

        private readonly BookCatalogService _bookCatalogService;

        // Фільтри, з якими показано поточну сторінку: «Попередня»/«Наступна» гортають саме цей результат
        private GetCatalogQuery _appliedQuery = new();
        private int _totalPages;

        public BookCatalogWindow(BookCatalogService bookCatalogService)
        {
            _bookCatalogService = bookCatalogService;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SortComboBox.ItemsSource = SortOptions;
            SortComboBox.SelectedIndex = 0;
            GenreComboBox.ItemsSource = new[] { AllGenres };
            GenreComboBox.SelectedIndex = 0;

            ShowButton.IsEnabled = false;
            ShowInfo("Завантаження каталогу...");

            try
            {
                var genresResult = await _bookCatalogService.GetGenresAsync();
                if (genresResult.IsSuccess)
                {
                    GenreComboBox.ItemsSource = new[] { AllGenres }.Concat(genresResult.Value).ToList();
                    GenreComboBox.SelectedIndex = 0;
                }
                else
                {
                    ShowError(genresResult.Error);
                }
            }
            catch (Exception)
            {
                // Подробиці помилки вже залоговані сервісом
                ShowError("Не вдалося завантажити список жанрів. Спробуйте пізніше.");
            }

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

        private GetCatalogQuery CreateQueryFromFilters(int pageNumber)
        {
            return new GetCatalogQuery
            {
                Genre = GenreComboBox.SelectedIndex > 0 ? GenreComboBox.SelectedItem as string : null,
                Year = YearTextBox.Text,
                SortBy = SortComboBox.SelectedValue is CatalogSortBy sortBy ? sortBy : CatalogSortBy.Title,
                PageNumber = pageNumber,
                PageSize = PageSize
            };
        }

        private static GetCatalogQuery WithPage(GetCatalogQuery query, int pageNumber)
        {
            return new GetCatalogQuery
            {
                Genre = query.Genre,
                Year = query.Year,
                SortBy = query.SortBy,
                PageNumber = pageNumber,
                PageSize = query.PageSize
            };
        }

        private async Task LoadPageAsync(GetCatalogQuery query)
        {
            SetControlsEnabled(false);
            ShowInfo("Завантаження...");

            try
            {
                var result = await _bookCatalogService.GetCatalogAsync(query);

                if (result.IsFailure)
                {
                    // Помилка валідації або завантаження: попередня сторінка лишається на екрані
                    ShowError(result.Error);
                    return;
                }

                var page = result.Value;
                _appliedQuery = query;
                _totalPages = page.TotalPages;

                BooksDataGrid.ItemsSource = page.Books;
                PageTextBlock.Text = page.TotalCount == 0
                    ? string.Empty
                    : $"Сторінка {page.PageNumber} з {page.TotalPages} · усього книг: {page.TotalCount}";
                ShowInfo(page.TotalCount == 0 ? "Книг за цими умовами не знайдено." : string.Empty);
            }
            catch (Exception)
            {
                // Сервіс сам повертає помилки як Result; сюди потрапляє лише непередбачене
                ShowError("Не вдалося завантажити каталог. Спробуйте пізніше.");
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            ShowButton.IsEnabled = isEnabled;
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

        private sealed record SortOption(string Name, CatalogSortBy Value);
    }
}
