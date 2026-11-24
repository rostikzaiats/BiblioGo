using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Libraries.Services
{
    // Сервіс підсистеми «Адміністрування системи» (філії мережі): валідація, бізнес-правила, логування, звернення до репозиторіїв.
    // Відмову (валідація чи бізнес-правило) повертає як неуспішний Result і логує як Warning; винятків не кидає
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
    }
}
