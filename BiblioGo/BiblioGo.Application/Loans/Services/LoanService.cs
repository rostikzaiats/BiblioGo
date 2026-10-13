using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Loans.Services
{
    // Сервіс підсистеми «Видача та повернення книг»: валідація, бізнес-правила, логування, звернення до репозиторіїв
    public class LoanService
    {
        private readonly ILoanRepository _loans;
        private readonly IBookCopyRepository _bookCopies;
        private readonly IUserRepository _users;
        private readonly ILogger<LoanService> _logger;

        public LoanService(
            ILoanRepository loans,
            IBookCopyRepository bookCopies,
            IUserRepository users,
            ILogger<LoanService> logger)
        {
            _loans = loans;
            _bookCopies = bookCopies;
            _users = users;
            _logger = logger;
        }

        // Use case 1: оформити заявку на отримання книги (повертає Id створеної заявки)
        public async Task<int> CreateRequestAsync(CreateLoanRequestCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = CreateLoanRequestValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Loan request rejected: {Reason}", error);
                throw new InputValidationException(error);
            }

            try
            {
                if (await _users.FindByIdAsync(command.UserId) is null)
                {
                    _logger.LogWarning("Loan request rejected: user {UserId} not found", command.UserId);
                    throw new NotFoundException("Користувача не знайдено");
                }

                if (await _loans.HasActiveLoanForBookAsync(command.UserId, command.BookId, cancellationToken))
                {
                    _logger.LogWarning(
                        "Loan request rejected: user {UserId} already has an active loan or request for book {BookId}",
                        command.UserId, command.BookId);
                    throw new BusinessRuleException("У вас вже є активна заявка або позика на цю книгу");
                }

                var copy = await _bookCopies.FindAvailableCopyAsync(command.BookId, command.LibraryId, cancellationToken);
                if (copy is null)
                {
                    _logger.LogWarning(
                        "Loan request rejected: no available copy of book {BookId} in library {LibraryId}",
                        command.BookId, command.LibraryId);
                    throw new BusinessRuleException("У цій філії немає вільних примірників цієї книги");
                }

                // Реальні дати видачі та повернення виставляє бібліотекар при підтвердженні (Use case 4),
                // до цього моменту IssueDate/DueDate містять час подання заявки
                var requestedAt = DateTime.UtcNow;
                var loan = new Loan
                {
                    UserId = command.UserId,
                    BookCopyId = copy.Id,
                    IssueDate = requestedAt,
                    DueDate = requestedAt,
                    Status = LoanStatus.Requested
                };

                var saved = await _loans.AddAsync(loan, cancellationToken);
                _logger.LogInformation(
                    "Loan request created {LoanId} for user {UserId}, book copy {BookCopyId}",
                    saved.Id, command.UserId, copy.Id);
                return saved.Id;
            }
            catch (Exception ex) when (ex is not BiblioGoException and not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to create loan request for user {UserId}, book {BookId}", command.UserId, command.BookId);
                throw;
            }
        }
    }
}
