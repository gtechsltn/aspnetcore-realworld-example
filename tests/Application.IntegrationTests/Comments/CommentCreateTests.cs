using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Features.Articles.Commands;
using Application.Features.Articles.Queries;
using Application.Features.Comments.Commands;
using Application.Features.Comments.Queries;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Application.IntegrationTests.Comments
{
    public class CommentCreateTests : TestBase
    {
        public CommentCreateTests(Startup factory, ITestOutputHelper output) : base(factory, output) { }

        public static IEnumerable<object[]> Data => new List<object[]>
        {
            new [] { new NewCommentDTO {
                Body = "",
            } },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public async Task CannotCreateCommentWithInvalidData(NewCommentDTO comment)
        {
            await ActingAs(new User
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
            });

            await Mediator.Send(new NewArticleRequest(
                new NewArticleDTO
                {
                    Title = "Test Title",
                    Description = "Test Description",
                    Body = "Test Body",
                }
            ));

            await this.Invoking(x => x.Act(new NewCommentRequest("test-title", comment)))
                .Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task CannotCreateCommentToNotExistingArticle()
        {
            await ActingAs(new User
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
            });

            await this.Invoking(x => x.Act(new NewCommentRequest(
                "slug-article", new NewCommentDTO
                {
                    Body = "Test Body",
                }
            )))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GuestCannotCreateComment()
        {
            await this.Invoking(x => x.Act(new NewCommentRequest(
                "slug-article", new NewCommentDTO()
            )))
                .Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task CanCreateComment()
        {
            await ActingAs(new User
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Bio = "My Bio",
                Image = "https://i.pravatar.cc/300"
            });

            await Mediator.Send(new NewArticleRequest(
                new NewArticleDTO
                {
                    Title = "Test Title",
                    Description = "Test Description",
                    Body = "Test Body",
                }
            ));

            var response = await Act(new NewCommentRequest("test-title", new NewCommentDTO
            {
                Body = "Thank you !",
            }));

            response.Comment.Should().BeEquivalentTo(new CommentDTO
            {
                Body = "Thank you !",
                Author = new AuthorDTO
                {
                    Username = "John Doe",
                    Bio = "My Bio",
                    Image = "https://i.pravatar.cc/300"
                },
            }, options => options.Excluding(x => x.Id).Excluding(x => x.CreatedAt).Excluding(x => x.UpdatedAt));

            (await Context.Comments.AnyAsync()).Should().BeTrue();
        }
    }
}
