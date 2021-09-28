using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Features.Articles.Commands;
using Application.Features.Comments.Commands;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Application.IntegrationTests.Articles
{
    public class ArticleDeleteTests : TestBase
    {
        public ArticleDeleteTests(Startup factory, ITestOutputHelper output) : base(factory, output) { }

        [Fact]
        public async Task GuestCannotDeleteArticle()
        {
            await this.Invoking(x => x.Act(new ArticleDeleteRequest("slug-article")))
                .Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task CannotDeleteNotExistingArticle()
        {
            await ActingAs(new User
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
            });

            await this.Invoking(x => x.Act(new ArticleDeleteRequest(
                "slug-article"
            )))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CannotDeleteArticleOfOtherAuthor()
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

            await ActingAs(new User
            {
                Name = "Jane Doe",
                Email = "jane.doe@example.com",
            });

            await this.Invoking(x => x.Act(new ArticleDeleteRequest(
                "test-title"
            )))
                .Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task CanDeleteOwnArticleWithAllComments()
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

            for (int i = 1; i <= 5; i++)
            {
                await Mediator.Send(new NewCommentRequest("test-title", new NewCommentDTO
                {
                    Body = $"This is John, Test Comment {i} !",
                }));
            }

            await Mediator.Send(new ArticleFavoriteRequest("test-title", true));

            await Act(new ArticleDeleteRequest("test-title"));

            (await Context.Articles.AnyAsync()).Should().BeFalse();
            (await Context.Comments.AnyAsync()).Should().BeFalse();
        }
    }
}
