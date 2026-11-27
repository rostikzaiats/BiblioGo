using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Dtos;
using BiblioGo.Application.Libraries.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BiblioGo.Wpf.ViewModels
{
    // ViewModel вікна «Редагувати інформацію про філію».
    public partial class EditLibraryViewModel : ObservableObject
    {
        private readonly LibraryService _libraryService;
        private readonly ICurrentUserSession _session;

        public EditLibraryViewModel(LibraryService libraryService, ICurrentUserSession session)
        {
            _libraryService = libraryService;
            _session = session;
        }

        [ObservableProperty]
        private IReadOnlyList<LibraryDto> _libraries = Array.Empty<LibraryDto>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private LibraryDto? _selectedLibrary;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _address = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _contactInfo = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        partial void OnSelectedLibraryChanged(LibraryDto? value)
        {
            Address = value?.Address ?? string.Empty;
            ContactInfo = value?.ContactInfo ?? string.Empty;
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            var result = await _libraryService.GetLibrariesAsync();
            if (result.IsFailure)
            {
                StatusMessage = result.Error;
                return;
            }

            Libraries = result.Value;
        }

        private bool CanSave()
        {
            return SelectedLibrary != null
                && !string.IsNullOrWhiteSpace(Address)
                && !string.IsNullOrWhiteSpace(ContactInfo);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            var user = _session.User;
            if (user is null)
            {
                StatusMessage = "Спочатку увійдіть.";
                return;
            }

            var library = SelectedLibrary;
            if (library is null)
            {
                StatusMessage = "Оберіть філію.";
                return;
            }

            var command = new UpdateLibraryCommand
            {
                ActorId = user.UserId,
                LibraryId = library.Id,
                Address = Address,
                ContactInfo = ContactInfo
            };

            var result = await _libraryService.UpdateLibraryAsync(command);
            if (result.IsFailure)
            {
                StatusMessage = result.Error;
                return;
            }

            var savedId = library.Id;
            await LoadAsync();
            SelectedLibrary = Libraries.FirstOrDefault(item => item.Id == savedId);
            StatusMessage = "Готово! Філію оновлено";
        }
    }
}
