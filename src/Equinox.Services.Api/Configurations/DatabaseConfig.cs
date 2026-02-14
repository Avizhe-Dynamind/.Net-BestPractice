using Equinox.Domain.Core.Events;
using Equinox.Domain.Events;
using Equinox.Domain.Models;
using Equinox.Infra.CrossCutting.Identity.Data;
using Equinox.Infra.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace Equinox.Services.Api.Configurations
{
    public static class DatabaseConfig
    {
        public static WebApplicationBuilder AddDatabaseConfiguration(this WebApplicationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var useSqlite = ShouldUseSqlite(builder.Configuration);

            if (useSqlite)
            {
                builder.Services.AddDbContext<EquinoxContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

                builder.Services.AddDbContext<EventStoreSqlContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

                return builder;
            }

            builder.Services.AddDbContext<EquinoxContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContext<EventStoreSqlContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            return builder;
        }


        private static bool ShouldUseSqlite(IConfiguration configuration)
        {
            var mode = configuration["Database:Mode"];
            return string.Equals(mode, "sqlite", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<WebApplication> UseDatabaseStartupTasks(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            var startupOptions = app.Configuration
                .GetSection(DatabaseStartupOptions.SectionName)
                .Get<DatabaseStartupOptions>() ?? new DatabaseStartupOptions();

            if (!startupOptions.ApplyMigrationsOnStartup)
            {
                return app;
            }

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var equinoxContext = services.GetRequiredService<EquinoxContext>();
            var eventStoreContext = services.GetRequiredService<EventStoreSqlContext>();
            var identityContext = services.GetRequiredService<EquinoxIdentityContext>();

            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

            await TryMigrateAsync(equinoxContext, nameof(EquinoxContext), logger);
            await TryMigrateAsync(eventStoreContext, nameof(EventStoreSqlContext), logger);
            await TryMigrateAsync(identityContext, nameof(EquinoxIdentityContext), logger);

            if (startupOptions.SeedOnStartup)
            {
                await EnsureSeedData(equinoxContext, eventStoreContext, identityContext);
            }

            return app;
        }

        private static async Task TryMigrateAsync(DbContext context, string contextName, ILogger logger)
        {
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(ex,
                    "Skipped automatic migration for {ContextName} because the EF model has pending changes. Add a migration for this context or disable DatabaseStartup:ApplyMigrationsOnStartup.",
                    contextName);
            }
        }

        private static async Task EnsureSeedData(
            EquinoxContext equinoxContext,
            EventStoreSqlContext eventStoreContext,
            EquinoxIdentityContext identityContext)
        {
            if (!identityContext.Users.Any())
            {
                var userId = Guid.NewGuid();
                var userEmail = "teste@teste.com";

                identityContext.Users.Add(new IdentityUser
                {
                    Id = userId.ToString(),
                    UserName = userEmail,
                    NormalizedUserName = userEmail.ToUpperInvariant(),
                    Email = userEmail,
                    NormalizedEmail = userEmail.ToUpperInvariant(),
                    AccessFailedCount = 0,
                    LockoutEnabled = false,
                    PasswordHash = "AQAAAAIAAYagAAAAEEdWhqiCwW/jZz0hEM7aNjok7IxniahnxKxxO5zsx2TvWs4ht1FUDnYofR8JKsA5UA==",
                    TwoFactorEnabled = false,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                });

                identityContext.UserClaims.Add(new IdentityUserClaim<string>
                {
                    UserId = userId.ToString(),
                    ClaimType = "Customers",
                    ClaimValue = "Write,Remove"
                });

                await identityContext.SaveChangesAsync();
            }

            if (!equinoxContext.Customers.Any())
            {
                var customer = new Customer(
                    Guid.NewGuid(),
                    "Eduardo Pires",
                    "contato@eduardopires.net.br",
                    new DateTime(1982, 4, 24));

                await equinoxContext.Customers.AddAsync(customer);
                await equinoxContext.SaveChangesAsync();

                var customerEvent = new CustomerRegisteredEvent(
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    customer.BirthDate);

                var serializedData = JsonSerializer.Serialize(customerEvent);
                await eventStoreContext.StoredEvent.AddAsync(new StoredEvent(customerEvent, serializedData, "teste@teste.com"));
                await eventStoreContext.SaveChangesAsync();
            }
        }
    }

    public class DatabaseStartupOptions
    {
        public const string SectionName = "DatabaseStartup";
        public bool ApplyMigrationsOnStartup { get; set; }
        public bool SeedOnStartup { get; set; }
    }
}
