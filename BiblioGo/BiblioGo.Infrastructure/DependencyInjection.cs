using BiblioGo.Application.Interfaces;
using BiblioGo.Infrastructure.Persistence;
using BiblioGo.Infrastructure.Repositories;
using BiblioGo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiblioGo.Infrastructure
{
    // Реєстрація бази даних та репозиторіїв шару Infrastructure у DI-контейнері
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            services.AddDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
            services.AddTransient<IBookRepository, BookRepository>();
            services.AddTransient<IBookCopyRepository, BookCopyRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ILoanRepository, LoanRepository>();
            services.AddTransient<ILibraryRepository, LibraryRepository>();
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            return services;
        }
    }
}
