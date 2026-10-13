using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

  namespace BiblioGo.Application.Users.Services
    {
        // Сервіс підсистеми «Авторизація та користувачі»: валідація, логування, звернення до репозиторію
        public class UserService
        {
            private readonly IUserRepository _users;
            private readonly IPasswordHasher _hasher;
            private readonly ILogger<UserService> _logger;

            public UserService(IUserRepository users, IPasswordHasher hasher, ILogger<UserService> logger)
            {
                _users = users;
                _hasher = hasher;
                _logger = logger;
            }

            // Use case 1: зареєструватися в системі
            public async Task<int> RegisterAsync(RegisterUserCommand command)
            {
                ArgumentNullException.ThrowIfNull(command);

                var error = RegisterUserValidator.Validate(command);
                if (error is not null)
                {
                    _logger.LogWarning("Registration rejected: {Reason}", error);
                    throw new InputValidationException(error);
                }

                var email = command.Email.Trim().ToLowerInvariant();

                try
                {
                    if (await _users.FindByEmailAsync(email) is not null)
                    {
                        _logger.LogWarning("Registration rejected: email already registered");
                        throw new BusinessRuleException("Користувач з такою поштою вже існує");
                    }

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = _hasher.Hash(command.Password),
                        FirstName = command.FirstName.Trim(),
                        LastName = command.LastName.Trim(),
                        Phone = command.Phone.Trim(),
                        Role = UserRole.Reader
                    };

                    var saved = await _users.AddAsync(user);
                    _logger.LogInformation("User registered {UserId}", saved.Id);
                    return saved.Id;
                }
                catch (Exception ex) when (ex is not BiblioGoException)
                {
                    _logger.LogError(ex, "Failed to register user");
                    throw;
                }
            }
        }
    }
