using BiblioGo.Application.Users.Dtos;

namespace BiblioGo.Application.Interfaces
{
    public interface ICurrentUserSession
    {
        UserDto? User { get; }
        void SignIn(UserDto user);
        void SignOut();
    }
}