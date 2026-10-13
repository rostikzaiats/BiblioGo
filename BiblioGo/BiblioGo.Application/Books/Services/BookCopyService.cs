using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Books.Services
{
    // Сервіс підсистеми «Управління бібліотечним фондом»: валідація, бізнес-правила, логування, звернення до репозиторію
    public class BookCopyService
    {
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly ILogger<BookCopyService> _logger;

        public BookCopyService(IBookCopyRepository bookCopyRepository, ILogger<BookCopyService> logger)
        {
            _bookCopyRepository = bookCopyRepository;
            _logger = logger;
        }

        // Use case 6: списати примірник (виданий або вже списаний примірник списати не можна)
        public async Task WriteOffAsync(WriteOffBookCopyCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.BookCopyId <= 0)
            {
                const string validationError = "Вкажіть коректний ідентифікатор примірника.";
                _logger.LogWarning("Write-off of book copy {BookCopyId} rejected: {Reason}", command.BookCopyId, validationError);
                throw new InputValidationException(validationError);
            }

            _logger.LogInformation("Writing off book copy {BookCopyId}", command.BookCopyId);

            try
            {
                var bookCopy = await _bookCopyRepository.GetByIdAsync(command.BookCopyId, cancellationToken);

                if (bookCopy is null)
                {
                    _logger.LogWarning("Write-off rejected: book copy {BookCopyId} not found", command.BookCopyId);
                    throw new NotFoundException($"Примірник {command.BookCopyId} не знайдено.");
                }

                var businessRuleError = GetWriteOffRestriction(bookCopy.Status);

                if (businessRuleError is not null)
                {
                    _logger.LogWarning(
                        "Write-off of book copy {BookCopyId} rejected: status is {Status}",
                        command.BookCopyId,
                        bookCopy.Status);
                    throw new BusinessRuleException(businessRuleError);
                }

                await _bookCopyRepository.SetStatusAsync(command.BookCopyId, BookCopyStatus.WrittenOff, cancellationToken);

                _logger.LogInformation("Book copy {BookCopyId} written off", command.BookCopyId);
            }
            catch (Exception ex) when (ex is not BiblioGoException and not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to write off book copy {BookCopyId}", command.BookCopyId);
                throw;
            }
        }

        private static string? GetWriteOffRestriction(BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Issued => "Не можна списати виданий примірник. Спочатку оформіть його повернення.",
                BookCopyStatus.WrittenOff => "Примірник уже списано.",
                _ => null
            };
        }
    }
}
