using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Dtos;
using BiblioGo.Application.Libraries.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Libraries.Services
{
    public class LibraryService
    {
        private readonly ILibraryRepository _libraries;
        private readonly IUserRepository _users;
        private readonly ILogger<LibraryService> _logger;

        public LibraryService(
            ILibraryRepository libraries,
            IUserRepository users,
            ILogger<LibraryService> logger)
        {
            _libraries = libraries;
            _users = users;
            _logger = logger;
        }

        // Use case 1: додати нову філію бібліотеки (лише Адміністратор; повертає Id створеної філії)
        public async Task<Result<int>> AddLibraryAsync(AddLibraryCommand command, CancellationToken cancellationToken = default)
        {
            var error = AddLibraryValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Adding library rejected: {Reason}", error);
                return Result.Failure<int>(error);
            }

            var actor = await _users.FindByIdAsync(command.ActorId);
            if (actor is null || actor.Role != UserRole.Administrator)
            {
                _logger.LogWarning("Adding library rejected: actor {ActorId} not found or not admin", command.ActorId);
                return Result.Failure<int>("Ви не маєте прав для додавання філії");
            }

            var name = command.Name.Trim();

            if (await _libraries.ExistsByNameAsync(name, cancellationToken))
            {
                _logger.LogWarning("Adding library rejected: library {Name} already exists", name);
                return Result.Failure<int>("Філія з такою назвою вже є в мережі");
            }

            var libraryId = await _libraries.AddAsync(
                new Library
                {
                    Name = name,
                    Address = command.Address.Trim(),
                    ContactInfo = command.ContactInfo.Trim()
                },
                cancellationToken);

            _logger.LogInformation("Library {LibraryId} added by administrator {ActorId}", libraryId, command.ActorId);
            return Result.Success(libraryId);
        }
        // Адміністрування use case 2: Редагувати інформацію про філію
        public async Task<Result>UpdateLibraryAsync(UpdateLibraryCommand command, CancellationToken cancellationToken = default)
        {
            var error = UpdateLibraryValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Updating library rejected: {Reason}", error);
                return Result.Failure(error);
            }
            var actor = await _users.FindByIdAsync(command.ActorId);
            if (actor is null || actor.Role != UserRole.Administrator)
            {
                _logger.LogWarning("Updating Library rejected: actor {ActorId} not found or not admin", command.ActorId);
                return Result.Failure("Ви не маєте прав для редагування філію");
            }

            var library = await _libraries.GetByIdAsync(command.LibraryId, cancellationToken);
            if (library is null)
            {
                _logger.LogWarning("Updating library rejected: {LibraryId} library not found", command.LibraryId);
                return Result.Failure("Філію не знайдено");
            }

            library.ContactInfo = command.ContactInfo.Trim();
            library.Address = command.Address.Trim();
            await _libraries.UpdateAsync(library, cancellationToken);
            _logger.LogInformation("Library updated: {LibraryId}", command.LibraryId);
            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<LibraryDto>>> GetLibrariesAsync(CancellationToken cancellationToken = default)
        {
            var libraries = await _libraries.GetAllAsync(cancellationToken);
            IReadOnlyList<LibraryDto> items = libraries
                .Select(library => new LibraryDto { Id = library.Id, Name = library.Name, Address = library.Address,
                    ContactInfo = library.ContactInfo})
                .ToList();

            _logger.LogInformation("Loaded {Count} libraries for dropdown", items.Count);
            return Result.Success(items);
        }
    }
}
