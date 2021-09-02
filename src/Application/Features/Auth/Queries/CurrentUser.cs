using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;

namespace Application.Features.Auth.Queries
{
    public class CurrentUserDTO
    {
        public string Email { get; set; }

        public string Username { get; set; }

        public string Bio { get; set; }

        public string Image { get; set; }

        public string Token { get; set; }
    }

    public record UserEnvelope(CurrentUserDTO User);

    public record CurrentUserQuery() : IAuthorizationRequest<UserEnvelope>;

    public class CurrentUserHandler : IAuthorizationRequestHandler<CurrentUserQuery, UserEnvelope>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public CurrentUserHandler(ICurrentUser currentUser, IMapper mapper)
        {
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public Task<UserEnvelope> Handle(CurrentUserQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UserEnvelope(
                _mapper.Map<User, CurrentUserDTO>(_currentUser.User)
            ));
        }
    }
}