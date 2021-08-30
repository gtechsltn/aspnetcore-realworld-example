using System.Threading.Tasks;
using Application.Interfaces;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;

namespace Application.IntegrationTests
{
    [Collection("DB")]
    public class TestBase : IAsyncLifetime, IClassFixture<Startup>
    {
        protected readonly IConfiguration _configuration;

        protected readonly IMediator _mediator;

        protected readonly AppDbContext _context;

        protected readonly IPasswordHasher _passwordHasher;

        protected readonly IJwtTokenGenerator _jwtTokenGenerator;

        public TestBase(Startup factory)
        {
            var provider = factory.Services.BuildServiceProvider();
            _configuration = factory.Configuration;

            _mediator = provider.GetService<IMediator>();
            _passwordHasher = provider.GetService<IPasswordHasher>();
            _jwtTokenGenerator = provider.GetService<IJwtTokenGenerator>();
            _context = provider.GetService<AppDbContext>();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            using (var conn = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                var checkpoint = new Checkpoint
                {
                    TablesToIgnore = new[] { "__EFMigrationsHistory" },
                    DbAdapter = DbAdapter.Postgres
                };
                await checkpoint.Reset(conn);
            }
        }
    }
}