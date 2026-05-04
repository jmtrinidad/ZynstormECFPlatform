using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Common.Enums;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class UserController(
    IAccountService accountService,
    IUserClientService userClientService,
    IMapper mapper,
    ILogger<UserController> logger) : ControllerBase
{
    private readonly IAccountService _accountService = accountService;
    private readonly IUserClientService _userClientService = userClientService;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UserController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<IEnumerable<UserViewDto>>> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            var userTypeClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var allUsers = await _accountService.GetAllUsersAsync();
            IEnumerable<User> usersToShow;
            
            // Si es SA (Super Admin), puede ver todos
            if (userTypeClaim?.ToUpper() == "SA" || userTypeClaim == "1")
            {
                usersToShow = allUsers;
            }
            else
            {
                // Si no, solo ve los que él creó
                usersToShow = allUsers.Where(u => u.CreatedByUserId == userId);
            }

            var viewDtos = _mapper.Map<IEnumerable<User>, List<UserViewDto>>(usersToShow);

            // Populate UserType for each DTO
            foreach (var dto in viewDtos)
            {
                var userEntity = allUsers.First(u => u.Id == dto.UserId);
                var userRole = await _accountService.GetRoleByUserAsync(userEntity);
                if (Enum.TryParse<UserType>(userRole?.Name, out var type))
                {
                    dto.UserType = type;
                }
            }

            return Ok(viewDtos);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<UserViewDto>> Post([FromBody] UserCreateDto dto)
    {
        try
        {
            var creatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var creatorRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedByUserId = creatorId
            };

            // Determinar el rol a asignar
            UserType roleToAssign = UserType.Normal; // Por defecto Normal (Id 3)
            
            // Solo SA puede asignar roles libremente
            if (creatorRole?.ToUpper() == "SA" || creatorRole == "1")
            {
                roleToAssign = (UserType)dto.RoleId;
            }

            var result = await _accountService.AddUserAsync(user, dto.Password ?? "DefaultPassword123!").ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _accountService.AddUserToRoleAsync(user, roleToAssign.ToString()).ConfigureAwait(false);

            // Asignar clientes
            if (dto.ClientIds != null && dto.ClientIds.Count > 0)
            {
                foreach (var clientId in dto.ClientIds)
                {
                    await _userClientService.InsertAsync(new UserClient
                    {
                        UserId = user.Id,
                        ClientId = clientId
                    });
                }
            }

            var viewDto = _mapper.Map<User, UserViewDto>(user);
            viewDto.UserType = roleToAssign;
            return Ok(viewDto);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<UserViewDto>> Put([FromBody] UserUpdateDto dto)
    {
        try
        {
            var user = await _accountService.GetUserByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Email = dto.Email;
            user.UserName = dto.UserName;
            user.IsActive = dto.IsActive;

            var result = await _accountService.UpdateUserAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Update Role if authorized
            var creatorRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (creatorRole?.ToUpper() == "SA" || creatorRole == "1")
            {
                var currentRole = await _accountService.GetRoleByUserAsync(user);
                if (currentRole?.Name != ((UserType)dto.RoleId).ToString())
                {
                    await _accountService.AddUserToRoleAsync(user, ((UserType)dto.RoleId).ToString());
                }
            }

            // Update Clients
            var existingClients = await _userClientService.GetManyByAsync(x => x.UserId == user.Id);
            foreach (var ec in existingClients)
            {
                await _userClientService.HardDeleteAsync(ec);
            }

            if (dto.ClientIds != null)
            {
                foreach (var clientId in dto.ClientIds)
                {
                    await _userClientService.InsertAsync(new UserClient { UserId = user.Id, ClientId = clientId });
                }
            }

            var viewDto = _mapper.Map<User, UserViewDto>(user);
            // Re-populate UserType
            var userRole = await _accountService.GetRoleByUserAsync(user);
            if (Enum.TryParse<UserType>(userRole?.Name, out var type))
            {
                viewDto.UserType = type;
            }

            return Ok(viewDto);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpDelete]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete([FromQuery] string id)
    {
        try
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
            user.DeletedTimeUtc = DateTime.UtcNow;
            await _accountService.UpdateUserAsync(user);

            return Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
