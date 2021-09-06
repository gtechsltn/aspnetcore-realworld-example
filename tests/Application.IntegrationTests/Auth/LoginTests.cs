using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Application.Features.Auth.Commands;
using Application.IntegrationTests.Events;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Application.IntegrationTests.Auth
{
    public class LoginTests : TestBase
    {
        public LoginTests(Startup factory, ITestOutputHelper output) : base(factory, output) { }

        public static IEnumerable<object[]> Data => new List<object[]>
        {
            new [] { new LoginDTO {
                Email = "jane.doe@example.com",
                Password = "password",
            } },
            new [] { new LoginDTO {
                Email = "john.doe@example.com",
                Password = "badpassword",
            } },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public async Task UserCannotLoginWithInvalidData(LoginDTO credentials)
        {
            await Context.Users.AddAsync(new User
            {
                Email = "john.doe@example.com",
                Name = "John Doe",
                Password = PasswordHasher.Hash("password"),
            });
            await Context.SaveChangesAsync();

            await this.Invoking(x => x.Act(new LoginCommand(credentials)))
                .Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task UserCanLogin()
        {
            var user = new User
            {
                Email = "john.doe@example.com",
                Name = "John Doe",
                Password = PasswordHasher.Hash("password"),
            };
            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();

            var currentUser = await Act(
                new LoginCommand(
                    new LoginDTO
                    {
                        Email = "john.doe@example.com",
                        Password = "password",
                    }
                )
            );

            currentUser.User.Username.Should().Be("John Doe");
            currentUser.User.Email.Should().Be("john.doe@example.com");

            var payload = JwtTokenGenerator.DecodeToken(currentUser.User.Token);

            payload["id"].Should().Be(user.Id.ToString(CultureInfo.InvariantCulture));
            payload["name"].Should().Be("John Doe");
            payload["email"].Should().Be("john.doe@example.com");
        }
    }
}
