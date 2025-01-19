using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnlineBookStore.Application.Common.DTO;
using OnlineBookStore.Application.Common.DTO.User;
using OnlineBookStore.Application.Common.Interfaces;
using OnlineBookStore.Entities.Models;
using OnlineBookStore.Infrastructure.Data;
using OnlineBookStore.Infrastructure.Data.DataAccess;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Numerics;
using System.Security.Claims;
using System.Text;

namespace OnlineBookStore.Infrastructure.Repository
{
    public class UserRepository(ApplicationDbContext db, IConfiguration configuration,
        ISqlDataAccess _sqlDataAccess) : IUserRepository
    {
        private readonly string spName = "stp_Users_API";
        private readonly ApplicationDbContext _db = db;
        private string secretKey = configuration.GetValue<string>("JWT:Secret");

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flag", "Login", DbType.String);
            parameters.Add("@userName", loginRequestDTO.userName, DbType.String);
            parameters.Add("@password", loginRequestDTO.password, DbType.String);

            var user = await _sqlDataAccess
                .GetData<userDTO, DynamicParameters>(spName, parameters);


            if (user == null)
            {
                return new TokenDTO()
                {
                    AccessToken = ""
                };
            }
            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);
            var refreshToken = await CreateNewRefreshToken(user.id.ToString(), jwtTokenId);
            TokenDTO tokenDto = new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return tokenDto;
        }

        private async Task<string> GetAccessToken(userDTO user, string jwtTokenId)
        {
            //if user was found generate JWT Token
            var roles = user.roleType;
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.firstName.ToString()),
                    new Claim(ClaimTypes.Role, roles),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                    new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Aud, configuration["JWT:Issuer"])
                }),
                Expires = DateTime.UtcNow.AddMinutes(100),
                Issuer = configuration["JWT:Issuer"],
                Audience = configuration["JWT:Audience"],
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;
        }

        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            // Find an existing refresh token
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            // Compare data from existing refresh and access token provided and if there is any missmatch then consider it as a fraud
            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            // When someone tries to use not valid refresh token, fraud possible
            if (!existingRefreshToken.IsValid)
            {
                await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            }
            // If just expired then mark as invalid and return empty
            if (existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            // replace old refresh with a new one with updated expire date
            var newRefreshToken = await CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);


            // revoke existing refresh token
            await MarkTokenAsInvalid(existingRefreshToken);

            // generate new access token

            var parameters = new DynamicParameters();
            parameters.Add("@flag", "GetUser", DbType.String);
            parameters.Add("@UserId", existingRefreshToken.UserId, DbType.String);

            var applicationUser = await _sqlDataAccess
                .GetData<userDTO, DynamicParameters>(spName, parameters);

            if (applicationUser == null)
                return new TokenDTO();

            var newAccessToken = await GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);

            return new TokenDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            };

        }

        public async Task RevokeRefreshToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(_ => _.Refresh_Token == tokenDTO.RefreshToken);

            if (existingRefreshToken == null)
                return;

            // Compare data from existing refresh and access token provided and
            // if there is any missmatch then we should do nothing with refresh token

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {

                return;
            }

            await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);

        }

        private async Task<string> CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
            };

            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }

        private bool GetAccessTokenData(string accessToken, string expectedUserId, string expectedTokenId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;
                return userId == expectedUserId && jwtTokenId == expectedTokenId;

            }
            catch
            {
                return false;
            }
        }


        private async Task MarkAllTokenInChainAsInvalid(string userId, string tokenId)
        {
            await _db.RefreshTokens.Where(u => u.UserId == userId
               && u.JwtTokenId == tokenId)
                   .ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));

        }


        private Task MarkTokenAsInvalid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
            return _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<userDTO>> GetUser(int? userId = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flag", "GetUser", DbType.String);
            parameters.Add("@UserId", userId, DbType.Int32);

            var users = await _sqlDataAccess
                .GetDataIEnumerable<userDTO, DynamicParameters>(spName, parameters);

            return users;
        }

        public async Task<IEnumerable<roleDTO>> GetRoleAsync()
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flag", "GetRole", DbType.String);

            var roles = await _sqlDataAccess
                .GetDataIEnumerable<roleDTO, DynamicParameters>(spName, parameters);

            return roles;
        }

        public async Task<string> CreateUserAsync(createUserDTO obj)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flag", "CreateUser", DbType.String);
            parameters.Add("@firstName", obj.firstName, DbType.String);
            parameters.Add("@lastName", obj.lastName, DbType.String);
            parameters.Add("@userName", obj.userName, DbType.String);
            parameters.Add("@password", obj.password, DbType.String);
            parameters.Add("@email", obj.email, DbType.String);
            parameters.Add("@phone", obj.phoneNo, DbType.String);

            var result = await _sqlDataAccess.GetData<string, DynamicParameters>(spName, parameters);

            return result;
        }

        public async Task<string> UpdateUserAsync(userDTO dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flag", "UpdateUser", DbType.String);
            parameters.Add("@userName", dto.userName, DbType.String);
            parameters.Add("@password", dto.password, DbType.String);

            var result = await _sqlDataAccess.GetData<string, DynamicParameters>(spName, parameters);

            return result;

        }
    }
}
