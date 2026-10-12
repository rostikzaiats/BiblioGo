using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Users.Handlers
{ 
    public class RegisterUserHandler
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ILogger<RegisterUserHandler> _logger;
        public RegisterUserHandler(
      IUserRepository users,
      IPasswordHasher hasher,
      ILogger<RegisterUserHandler> logger)
        {
            _users = users;
            _hasher = hasher;
            _logger = logger;
        }

        public async Task<int> Handle(RegisterUserCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = RegisterUserValidator.Validate(command);
            if (error != null)
            {
                _logger.LogWarning("Registration rejected: {Reason}", error);
                throw new InputValidationException(error);
            }

            var email = command.Email.Trim().ToLowerInvariant();

            if (await _users.FindByEmailAsync(email) != null)
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
    }
}