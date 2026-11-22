using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;

namespace BiblioGo.Wpf.Views
{
    // Юз кейс «Переглянути каталог книг»: каталог постійно показаний на головному екрані (розділ «Каталог»).
    // Фільтри за жанром і роком, сортування, перемикання сторінок
    public partial class BookCatalogView : UserControl
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
        private GetCatalogQuery _appliedQuery = new() { PageSize = PageSize };
        private int _totalPages;
        private bool _isLoaded;

        public BookCatalogView(BookCatalogService bookCatalogService)
        {
            _bookCatalogService = bookCatalogService;
            InitializeComponent();
        }

        // Loaded спрацьовує щоразу, коли контрол знову потрапляє на екран, тому завантажуємо лише перший раз
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            SortComboBox.ItemsSource = SortOptions;
            SortComboBox.SelectedIndex = 0;
            GenreComboBox.ItemsSource = new[] { AllGenres };
            GenreComboBox.SelectedIndex = 0;

            ShowInfo("Завантаження каталогу...");
            await LoadGenresAsync();
            await LoadPageAsync(CreateQueryFromFilters(pageNumber: 1));
        }

        // Оновити каталог із тими самими фільтрами і сторінкою (наприклад, після змін у фонді)
        public async Task RefreshAsync()
        {
            if (!_isLoaded)
            {
                return;
            }

            await LoadGenresAsync();
            await LoadPageAsync(_appliedQuery);

            // Книг стало менше і поточної сторінки вже немає — показуємо останню
            if (_totalPages > 0 && _appliedQuery.PageNumber > _totalPages)
            {
                await LoadPageAsync(WithPage(_appliedQuery, _totalPages));
            }
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

        // Жанри для випадайки; обраний жанр лишається обраним, якщо він досі є в каталозі
        private async Task LoadGenresAsync()
        {
            var selectedGenre = GenreComboBox.SelectedIndex > 0 ? GenreComboBox.SelectedItem as string : null;

            var genresResult = await _bookCatalogService.GetGenresAsync();
            if (genresResult.IsFailure)
            {
                ShowError(genresResult.Error);
                return;
            }

            var genres = new[] { AllGenres }.Concat(genresResult.Value).ToList();
            GenreComboBox.ItemsSource = genres;
            GenreComboBox.SelectedIndex = selectedGenre is not null && genres.Contains(selectedGenre)
                ? genres.IndexOf(selectedGenre)
                : 0;
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

            var result = await _bookCatalogService.GetCatalogAsync(query);

            if (result.IsFailure)
            {
                // Помилка валідації: попередня сторінка лишається на екрані
                SetControlsEnabled(true);
                ShowError(result.Error);
                return;
            }

            var page = result.Value;
            _appliedQuery = query;
            _totalPages = page.TotalPages;
            SetControlsEnabled(true);

            BooksDataGrid.ItemsSource = page.Books;
            PageTextBlock.Text = page.TotalCount == 0
                ? string.Empty
                : $"Сторінка {page.PageNumber} з {page.TotalPages} · усього книг: {page.TotalCount}";
            ShowInfo(page.TotalCount == 0 ? "Книг за цими умовами не знайдено." : string.Empty);
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            ShowButton.IsEnabled = isEnabled;
            PreviousPageButton.IsEnabled = isEnabled && _appliedQuery.PageNumber > 1 && _totalPages > 0;
            NextPageButton.IsEnabled = isEnabled && _appliedQuery.PageNumber < _totalPages;
        }

        private void ShowInfo(string message)
        {
            StatusTextBlock.Foreground = (Brush)FindResource("TextSecondaryBrush");
            StatusTextBlock.Text = message;
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Foreground = (Brush)FindResource("ErrorBrush");
            StatusTextBlock.Text = message;
        }

        private sealed record SortOption(string Name, CatalogSortBy Value);
    }
}
