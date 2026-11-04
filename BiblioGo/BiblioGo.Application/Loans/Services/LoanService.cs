using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Dtos;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Loans.Services
{
    // Сервіс підсистеми «Видача та повернення книг»: валідація, бізнес-правила, логування, звернення до репозиторіїв.
    // Помилку повертає як неуспішний Result, тож вікна не ловлять винятки
    public class LoanService
    {
        private const string ConcurrentChangeError = "Стан заявки або примірника щойно змінився. Оновіть дані та спробуйте ще раз";

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
        public async Task<Result<int>> CreateRequestAsync(CreateLoanRequestCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = CreateLoanRequestValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Loan request rejected: {Reason}", error);
                return Result.Failure<int>(error);
            }

            try
            {
                if (await _users.FindByIdAsync(command.UserId) is null)
                {
                    _logger.LogWarning("Loan request rejected: user {UserId} not found", command.UserId);
                    return Result.Failure<int>("Користувача не знайдено");
                }

                if (await _loans.HasActiveLoanForBookAsync(command.UserId, command.BookId, cancellationToken))
                {
                    _logger.LogWarning(
                        "Loan request rejected: user {UserId} already has an active loan or request for book {BookId}",
                        command.UserId, command.BookId);
                    return Result.Failure<int>("У вас вже є активна заявка або позика на цю книгу");
                }

                var copy = await _bookCopies.FindAvailableCopyAsync(command.BookId, command.LibraryId, cancellationToken);
                if (copy is null)
                {
                    _logger.LogWarning(
                        "Loan request rejected: no available copy of book {BookId} in library {LibraryId}",
                        command.BookId, command.LibraryId);
                    return Result.Failure<int>("У цій філії немає вільних примірників цієї книги");
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
                return Result.Success(saved.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to create loan request for user {UserId}, book {BookId}", command.UserId, command.BookId);
                return Result.Failure<int>("Не вдалося оформити заявку. Спробуйте пізніше.");
            }
        }

        // Use case 2: переглянути власні позики та терміни повернення (нові першими)
        public async Task<Result<IReadOnlyList<LoanDto>>> GetUserLoansAsync(GetUserLoansQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var error = GetUserLoansValidator.Validate(query);
            if (error is not null)
            {
                _logger.LogWarning("Loans view for user {UserId} rejected: {Reason}", query.UserId, error);
                return Result.Failure<IReadOnlyList<LoanDto>>(error);
            }

            _logger.LogInformation("Getting loans of user {UserId}, status filter {Status}", query.UserId, query.Status);

            try
            {
                if (await _users.FindByIdAsync(query.UserId) is null)
                {
                    _logger.LogWarning("Loans view rejected: user {UserId} not found", query.UserId);
                    return Result.Failure<IReadOnlyList<LoanDto>>("Користувача не знайдено");
                }

                var loans = await _loans.GetByUserIdAsync(query.UserId, query.Status, cancellationToken);

                var now = DateTime.UtcNow;
                IReadOnlyList<LoanDto> result = loans.Select(loan => ToDto(loan, now)).ToList();

                _logger.LogInformation("Found {LoanCount} loan(s) of user {UserId}", result.Count, query.UserId);
                return Result.Success(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to get loans of user {UserId}", query.UserId);
                return Result.Failure<IReadOnlyList<LoanDto>>("Не вдалося завантажити позики. Спробуйте пізніше.");
            }
        }

        // Use case 4: підтвердити та оформити видачу книги (бібліотекар задає граничну дату повернення).
        // Позика Requested -> Issued, примірник Available -> Issued
        public async Task<Result> ConfirmIssueAsync(ConfirmLoanIssueCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = ConfirmLoanIssueValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning(
                    "Loan issue confirmation rejected: {Reason} (loan {LoanId}, librarian {LibrarianId})",
                    error, command.LoanId, command.LibrarianId);
                return Result.Failure(error);
            }

            _logger.LogInformation("Confirming issue of loan {LoanId} by librarian {LibrarianId}", command.LoanId, command.LibrarianId);

            try
            {
                var librarianError = await CheckLibrarianAsync(command.LibrarianId);
                if (librarianError is not null)
                {
                    _logger.LogWarning(
                        "Loan issue confirmation rejected: {Reason} (librarian {LibrarianId})",
                        librarianError, command.LibrarianId);
                    return Result.Failure(librarianError);
                }

                var loan = await _loans.GetByIdAsync(command.LoanId, cancellationToken);
                if (loan is null)
                {
                    _logger.LogWarning("Loan issue confirmation rejected: loan {LoanId} not found", command.LoanId);
                    return Result.Failure("Заявку не знайдено");
                }

                var loanError = GetIssueRestriction(loan.Status);
                if (loanError is not null)
                {
                    _logger.LogWarning(
                        "Loan issue confirmation rejected: loan {LoanId} status is {Status}",
                        loan.Id, loan.Status);
                    return Result.Failure(loanError);
                }

                var copy = await _bookCopies.GetByIdAsync(loan.BookCopyId, cancellationToken);
                if (copy is null)
                {
                    _logger.LogWarning(
                        "Loan issue confirmation rejected: book copy {BookCopyId} of loan {LoanId} not found",
                        loan.BookCopyId, loan.Id);
                    return Result.Failure("Примірник не знайдено");
                }

                var copyError = GetCopyIssueRestriction(copy.Status);
                if (copyError is not null)
                {
                    _logger.LogWarning(
                        "Loan issue confirmation rejected: book copy {BookCopyId} status is {Status}",
                        copy.Id, copy.Status);
                    return Result.Failure(copyError);
                }

                var dueDate = ToEndOfDayUtc(command.DueDate);
                if (!await _loans.ConfirmIssueAsync(loan.Id, copy.Id, DateTime.UtcNow, dueDate, cancellationToken))
                {
                    _logger.LogWarning(
                        "Loan issue confirmation rejected: loan {LoanId} or book copy {BookCopyId} was changed concurrently",
                        loan.Id, copy.Id);
                    return Result.Failure(ConcurrentChangeError);
                }

                _logger.LogInformation(
                    "Loan {LoanId} issued by librarian {LibrarianId}, due {DueDate:O}",
                    loan.Id, command.LibrarianId, dueDate);
                return Result.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to confirm issue of loan {LoanId}", command.LoanId);
                return Result.Failure("Не вдалося підтвердити видачу. Спробуйте пізніше.");
            }
        }

        // Use case 5: оформити повернення книги. Позика Issued -> Returned, примірник Issued -> Available
        public async Task<Result> ReturnBookAsync(ReturnBookCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = ReturnBookValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning(
                    "Book return rejected: {Reason} (loan {LoanId}, librarian {LibrarianId})",
                    error, command.LoanId, command.LibrarianId);
                return Result.Failure(error);
            }

            _logger.LogInformation("Registering return of loan {LoanId} by librarian {LibrarianId}", command.LoanId, command.LibrarianId);

            try
            {
                var librarianError = await CheckLibrarianAsync(command.LibrarianId);
                if (librarianError is not null)
                {
                    _logger.LogWarning("Book return rejected: {Reason} (librarian {LibrarianId})", librarianError, command.LibrarianId);
                    return Result.Failure(librarianError);
                }

                var loan = await _loans.GetByIdAsync(command.LoanId, cancellationToken);
                if (loan is null)
                {
                    _logger.LogWarning("Book return rejected: loan {LoanId} not found", command.LoanId);
                    return Result.Failure("Заявку не знайдено");
                }

                var loanError = GetReturnRestriction(loan.Status);
                if (loanError is not null)
                {
                    _logger.LogWarning("Book return rejected: loan {LoanId} status is {Status}", loan.Id, loan.Status);
                    return Result.Failure(loanError);
                }

                if (!await _loans.ReturnAsync(loan.Id, loan.BookCopyId, DateTime.UtcNow, cancellationToken))
                {
                    _logger.LogWarning(
                        "Book return rejected: loan {LoanId} or book copy {BookCopyId} was changed concurrently",
                        loan.Id, loan.BookCopyId);
                    return Result.Failure(ConcurrentChangeError);
                }

                _logger.LogInformation("Loan {LoanId} returned, registered by librarian {LibrarianId}", loan.Id, command.LibrarianId);
                return Result.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to register return of loan {LoanId}", command.LoanId);
                return Result.Failure("Не вдалося оформити повернення. Спробуйте пізніше.");
            }
        }

        // Видачу та повернення оформлює лише бібліотекар; повертає текст помилки або null
        private async Task<string?> CheckLibrarianAsync(int librarianId)
        {
            var librarian = await _users.FindByIdAsync(librarianId);

            if (librarian is null)
            {
                return "Бібліотекаря не знайдено";
            }

            return librarian.Role == UserRole.Librarian ? null : "Цю дію може виконувати лише бібліотекар";
        }

        private static string? GetIssueRestriction(LoanStatus status)
        {
            return status switch
            {
                LoanStatus.Issued => "Книгу за цією заявкою вже видано",
                LoanStatus.Returned => "Заявку вже закрито: книгу повернено",
                _ => null
            };
        }

        private static string? GetCopyIssueRestriction(BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Issued => "Примірник уже виданий іншому читачу",
                BookCopyStatus.WrittenOff => "Примірник списано, видати його не можна",
                _ => null
            };
        }

        private static string? GetReturnRestriction(LoanStatus status)
        {
            return status switch
            {
                LoanStatus.Requested => "Книгу за цією заявкою ще не видано",
                LoanStatus.Returned => "Книгу за цією заявкою вже повернено",
                _ => null
            };
        }

        // Книгу можна повернути протягом усього граничного дня, тому термін — кінець цієї дати
        // за локальним часом (у БД дати зберігаються в UTC)
        private static DateTime ToEndOfDayUtc(DateTime date)
        {
            var endOfDay = DateTime.SpecifyKind(date.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Local);
            return endOfDay.ToUniversalTime();
        }

        private static LoanDto ToDto(Loan loan, DateTime now)
        {
            // Для заявки Requested дати ще не виставлені бібліотекарем (у них лише час подання заявки)
            var datesAssigned = loan.Status != LoanStatus.Requested;

            return new LoanDto
            {
                LoanId = loan.Id,
                BookTitle = loan.BookCopy.Book.Title,
                BookAuthor = loan.BookCopy.Book.Author,
                LibraryName = loan.BookCopy.Library.Name,
                InventoryNumber = loan.BookCopy.InventoryNumber,
                Status = loan.Status,
                IssueDate = datesAssigned ? loan.IssueDate : null,
                DueDate = datesAssigned ? loan.DueDate : null,
                ReturnDate = loan.ReturnDate,
                IsOverdue = loan.Status == LoanStatus.Issued && loan.DueDate < now
            };
        }
    }
}
