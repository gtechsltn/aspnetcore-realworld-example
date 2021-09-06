using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.Features.Auth.Commands;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Application.IntegrationTests.Auth
{
    public class RegisterTests : TestBase
    {
        public RegisterTests(Startup factory, ITestOutputHelper output) : base(factory, output) { }

        public static IEnumerable<object[]> Data => new List<object[]>
        {
            new [] { new RegisterDTO {
                Email = "john.doe",
                Username = "John Doe",
                Password = "password",
            } },
            new [] { new RegisterDTO {
                Email = "john.doe@example.com",
            } },
            new [] { new RegisterDTO {
                Email = "john.doe@example.com",
                Username = "John Doe",
                Password = "pass",
            } },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public async Task UserCannotRegisterWithInvalidData(RegisterDTO user)
        {
            await this.Invoking(x => x.Act(new RegisterCommand(user)))
                .Should().ThrowAsync<ValidationException>()
                .Where(e => e.Errors.Any());
        }

        [Fact]
        public async Task UserCanRegister()
        {
            var request = new RegisterCommand(new RegisterDTO
            {
                Email = "john.doe@example.com",
                Username = "John Doe",
                Password = "password",
            });

            var currentUser = await Act(request);

            currentUser.User.Username.Should().Be("John Doe");
            currentUser.User.Email.Should().Be("john.doe@example.com");

            var created = await Context.Users.Where(u => u.Email == request.User.Email).SingleOrDefaultAsync();
            created.Should().NotBeNull();

            PasswordHasher.Check("password", created.Password).Should().BeTrue();

            var payload = JwtTokenGenerator.DecodeToken(currentUser.User.Token);

            payload["id"].Should().Be(created.Id.ToString(CultureInfo.InvariantCulture));
            payload["name"].Should().Be("John Doe");
            payload["email"].Should().Be("john.doe@example.com");
        }

        [Fact]
        public async Task UserCannotRegisterTwice()
        {
            await Context.Users.AddAsync(new User
            {
                Email = "john.doe@example.com",
                Name = "John Doe",
                Password = "password",
            });
            await Context.SaveChangesAsync();

            await this.Invoking(x => x.Act(
                new RegisterCommand(
                    new RegisterDTO
                    {
                        Email = "john.doe@example.com",
                        Username = "John Doe",
                        Password = "password",
                    }
                )))
                    .Should().ThrowAsync<ValidationException>()
                    .Where(e => e.Errors.First(x => x.PropertyName == "User.Email")
                        .ErrorMessage == "Email is already used");
        }
    }
}
