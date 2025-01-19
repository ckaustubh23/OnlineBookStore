using OnlineBookStore.Application.Common.DTO;
using OnlineBookStore.Application.Common.DTO.User;

namespace OnlineBookStore.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO);
        Task RevokeRefreshToken(TokenDTO tokenDTO);
        Task<IEnumerable<userDTO>> GetUser(int? userId = null);
        Task<IEnumerable<roleDTO>> GetRoleAsync();
        Task<string> CreateUserAsync(createUserDTO obj);
    }
}
