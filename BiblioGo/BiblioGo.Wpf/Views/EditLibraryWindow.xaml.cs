using System.Windows;
using BiblioGo.Wpf.ViewModels;

namespace BiblioGo.Wpf.Views
{
    // Вікно юз кейсу «Редагувати інформацію про філію».
    // Уся логіка у ViewModel
    public partial class EditLibraryWindow : Window
    {
        private readonly EditLibraryViewModel _viewModel;

        public EditLibraryWindow(EditLibraryViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}