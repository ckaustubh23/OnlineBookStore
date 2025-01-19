using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineBookStore.Application.Common.DTO;
using OnlineBookStore.Application.Common.DTO.User;
using OnlineBookStore.Application.Common.Interfaces;
using OnlineBookStore.Entities.Models;
using System.Net;

namespace OnlineBookStore.Controllers
{
    public class UsersController(IUnitOfWork _unitOfWork) : ApiController
    {
        protected APIResponse _response = new();

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            var tokenDto = await _unitOfWork.UserRepository.Login(model);
            if (tokenDto == null || string.IsNullOrEmpty(tokenDto.AccessToken))
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username or password is incorrect");
                return BadRequest(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = tokenDto;
            return Ok(_response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> GetNewTokenFromRefreshToken([FromBody] TokenDTO tokenDTO)
        {
            if (ModelState.IsValid)
            {
                var tokenDTOResponse = await _unitOfWork.UserRepository.RefreshAccessToken(tokenDTO);
                if (tokenDTOResponse == null || string.IsNullOrEmpty(tokenDTOResponse.AccessToken))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Token Invalid");
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDTOResponse;
                return Ok(_response);
            }
            else
            {
                _response.IsSuccess = false;
                _response.Result = "Invalid Input";
                return BadRequest(_response);
            }

        }


        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] TokenDTO tokenDTO)
        {

            if (ModelState.IsValid)
            {
                await _unitOfWork.UserRepository.RevokeRefreshToken(tokenDTO);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            _response.IsSuccess = false;
            _response.Result = "Invalid Input";
            return BadRequest(_response);
        }

        //[Authorize]
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers(int? userId = null)
        {
            var userDto = await _unitOfWork.UserRepository.GetUser(userId);

            if (userDto == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add("Users Not Available.");
                _response.IsSuccess = false;
                return NotFound(_response);
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = userDto;
            return Ok(_response);
        }

        //[Authorize]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] createUserDTO dTO)
        {
            var result = await _unitOfWork.UserRepository.CreateUserAsync(dTO);

            if (result == null)
            {
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add("User Already Exist.");
                _response.IsSuccess = false;
                return Conflict(_response);
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = result;
            return Ok(_response);
        }
    }
}
