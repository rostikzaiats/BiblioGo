using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Dtos;
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
        public async Task<Result<int>> RegisterAsync(RegisterUserCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = RegisterUserValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Registration rejected: {Reason}", error);
                return Result.Failure<int>(error);
            }

            var email = command.Email.Trim().ToLowerInvariant();

            try
            {
                if (await _users.FindByEmailAsync(email) is not null)
                {
                    _logger.LogWarning("Registration rejected: email already registered");
                    return Result.Failure<int>("Користувач з такою поштою вже зареєстрований");
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
                return Result.Success(saved.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register user");
                return Result.Failure<int>("Не вдалося зареєструвати користувача. Спробуйте пізніше.");
            }
        }

        // Use case 2: увійти в систему
        public async Task<Result<UserDto>> LoginAsync(LoginUserCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            var error = LoginUserValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Login rejected: {Reason}", error);
                return Result.Failure<UserDto>(error);
            }
            var email = command.Email.Trim().ToLowerInvariant();
            try
            {
                var user = await _users.FindByEmailAsync(email);
                if (user is null || !_hasher.Verify(command.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login rejected: invalid credentials");
                    return Result.Failure<UserDto>("Невірна пошта або пароль");
                }
                _logger.LogInformation("User logged in {UserId}", user.Id);
                return Result.Success(new UserDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log in user");
                return Result.Failure<UserDto>("Не вдалося виконати вхід. Спробуйте пізніше.");
            }
        }

        // Use case 3: оновити користувача
        public async Task<Result> UpdateUserAsync(UpdateUserCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            var error = UpdateUserValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Update user rejected: {Reason}", error);
                return Result.Failure(error);
            }
            try
            {
                var user = await _users.FindByIdAsync(command.UserId);
                if (user is null)
                {
                    _logger.LogWarning("Update user rejected: user not found");
                    return Result.Failure("Користувача не знайдено");
                }
                user.FirstName = command.FirstName.Trim();
                user.LastName = command.LastName.Trim();
                user.Phone = command.Phone.Trim();
                await _users.UpdateAsync(user);
                _logger.LogInformation("User updated {UserId}", user.Id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user");
                return Result.Failure("Не вдалося оновити користувача. Спробуйте пізніше.");
            }
        }

        // Use case 4: змінити пароль
        public async Task<Result> ChangePasswordAsync(ChangePasswordCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            var error = ChangePasswordValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Change password rejected: {Reason}", error);
                return Result.Failure(error);
            }

            try
            {
                var user = await _users.FindByIdAsync(command.UserId);
                if (user is null)
                {
                    _logger.LogWarning("Change password rejected: user not found");
                    return Result.Failure("Користувача не знайдено");
                }

                if (!_hasher.Verify(command.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Change password rejected: wrong current password for user {UserId}", user.Id);
                    return Result.Failure("Поточний пароль введено невірно");
                }

                user.PasswordHash = _hasher.Hash(command.NewPassword);
                await _users.UpdateAsync(user);
                _logger.LogInformation("Password changed for user {UserId}", user.Id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password");
                return Result.Failure("Не вдалося змінити пароль. Спробуйте пізніше.");
            }
        }
        // Use case 5: змінити роль користувача
        public async Task<Result> ChangeUserRoleAsync(ChangeUserRoleCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            var error = ChangeUserRoleValidator.Validate(command);
            if (error is not null)
            {
                _logger.LogWarning("Change user role rejected: {Reason}", error);
                return Result.Failure(error);
            }
            try
            {
                var actor = await _users.FindByIdAsync(command.ActorId);
                if (actor is null || actor.Role != UserRole.Administrator)
                {
                    _logger.LogWarning("Change user role rejected: actor not found or not admin");
                    return Result.Failure("Ви не маєте прав для зміни ролі користувача");
                }
                var target = await _users.FindByEmailAsync(command.TargetEmail.Trim().ToLowerInvariant());
                if (target is null)
                {
                    _logger.LogWarning("Change user role rejected: user not found");
                    return Result.Failure("Користувача не знайдено");
                }
                if (target.Id == actor.Id)
                {
                    _logger.LogWarning("Change user role rejected: admin cannot change own role");
                    return Result.Failure("Ви не можете змінювати власну роль");
                }
                if (target.Role == command.NewRole)
                {
                    _logger.LogWarning("Change user role rejected: cannot change role of another admin");
                    return Result.Failure("Користувач вже має цю роль");
                }
                target.Role = command.NewRole;
                await _users.UpdateAsync(target);
                _logger.LogInformation("User role changed for user {UserId} to {NewRole}", target.Id, target.Role);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change user role");
                return Result.Failure("Не вдалося змінити роль користувача. Спробуйте пізніше.");
            }
        }
    }
}