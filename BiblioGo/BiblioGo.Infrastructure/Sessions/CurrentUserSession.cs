using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Dtos;

namespace BiblioGo.Infrastructure.Sessions
{
    sealed class CurrentUserSession : ICurrentUserSession
    {
        public UserDto? User { get; private set; }
        public void SignIn(UserDto user)
        {
            User = user;
        }

        public void SignOut()
        {
            User =  null;
        }
    }
}