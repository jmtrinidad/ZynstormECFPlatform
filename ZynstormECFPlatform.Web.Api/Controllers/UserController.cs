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
using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class UserController(
    IAccountService accountService,
    IUserClientService userClientService,
    IUserPageAccessManager pageAccessManager,
    IMapper mapper,
    ILogger<UserController> logger) : ControllerBase
{
    private readonly IAccountService _accountService = accountService;
    private readonly IUserClientService _userClientService = userClientService;
    private readonly IUserPageAccessManager _pageAccessManager = pageAccessManager;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UserController> _logger = logger;

    private bool CallerIsSA
    {
        get
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role?.ToUpper() == "SA" || role == "1";
        }
    }

    /// <summary>Clientes que ve quien llama (sus UserClient). Null = SA, sin restricción.</summary>
    private async Task<HashSet<int>?> AllowedClientIdsAsync()
    {
        if (CallerIsSA) return null;

        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var links = await _userClientService.GetManyByAsync(x => x.UserId == callerId);

        return links.Select(x => x.ClientId).ToHashSet();
    }

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

            // Clientes asignados a cada usuario (una sola consulta). Sin esto el formulario
            // de edición los recibía vacíos y, al guardar, el PUT borraba todas las asignaciones.
            var shownUserIds = viewDtos.Select(d => d.UserId).ToList();
            var userClients = await _userClientService.GetManyByAsync(x => shownUserIds.Contains(x.UserId));
            var clientIdsByUser = userClients
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ClientId).Distinct().ToList());

            // Populate UserType and ClientIds for each DTO
            foreach (var dto in viewDtos)
            {
                dto.ClientIds = clientIdsByUser.TryGetValue(dto.UserId, out var assignedClientIds) ? assignedClientIds : [];

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

            // Un Admin solo asigna clientes que él mismo ve (en un revendedor, los de su cuenta).
            var forbiddenClients = UserClientAssignmentPolicy.FindForbidden(dto.ClientIds, await AllowedClientIdsAsync());
            if (forbiddenClients.Count > 0)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = UserClientAssignmentPolicy.ForbiddenMessage, clientIds = forbiddenClients });

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
            // SA edita a cualquiera; un Admin solo a los usuarios que creó.
            var user = await GetManageableUserAsync(dto.UserId);
            if (user == null) return NotFound();

            var allowedClientIds = await AllowedClientIdsAsync();
            var forbiddenClients = UserClientAssignmentPolicy.FindForbidden(dto.ClientIds, allowedClientIds);
            if (forbiddenClients.Count > 0)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = UserClientAssignmentPolicy.ForbiddenMessage, clientIds = forbiddenClients });

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

            // Update Clients: un Admin solo reemplaza las asignaciones que él mismo ve.
            var existingClients = (await _userClientService.GetManyByAsync(x => x.UserId == user.Id)).ToList();
            var replaceable = UserClientAssignmentPolicy.ClientIdsToReplace(existingClients.Select(x => x.ClientId), allowedClientIds);
            foreach (var ec in existingClients.Where(x => replaceable.Contains(x.ClientId)))
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
            viewDto.ClientIds = dto.ClientIds ?? [];
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

    [HttpGet("page-access")]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<UserPageAccessDto>> GetPageAccess([FromQuery] string userId)
    {
        try
        {
            var user = await GetManageableUserAsync(userId);
            if (user == null) return NotFound();

            var role = await _accountService.GetRoleByUserAsync(user);

            return Ok(new UserPageAccessDto
            {
                PageAccess = await _pageAccessManager.GetAsync(user, role?.Name),
                HomePagePath = user.HomePagePath
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("page-access")]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)]
    public async Task<ActionResult> UpdatePageAccess([FromQuery] string userId, [FromBody] UserPageAccessDto dto)
    {
        try
        {
            var user = await GetManageableUserAsync(userId);
            if (user == null) return NotFound();

            var role = await _accountService.GetRoleByUserAsync(user);
            await _pageAccessManager.SaveAsync(user, role?.Name, dto.PageAccess, dto.HomePagePath);

            return Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    // Mismo alcance que el listado: SA ve a todos; Admin solo a los usuarios que creó.
    private async Task<User?> GetManageableUserAsync(string userId)
    {
        var user = await _accountService.GetUserByIdAsync(userId);
        if (user == null) return null;

        var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (callerRole?.ToUpper() == "SA" || callerRole == "1") return user;

        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return user.CreatedByUserId == callerId ? user : null;
    }

    [HttpDelete]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete([FromQuery] string id)
    {
        try
        {
            // SA borra a cualquiera; un Admin solo a los usuarios que creó.
            var user = await GetManageableUserAsync(id);
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
