using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BiblioGo.Application
{
    // Реєстрація сервісів шару Application у DI-контейнері
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<BookCatalogService>();
            services.AddTransient<UserService>();
            return services;
        }
    }
}
