using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public NotificationController(
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<NotificationTypeDto>>> GetTypes()
    {
        var types = await _notificationService.GetNotificationTypesAsync();
        return Ok(_mapper.Map<IEnumerable<NotificationTypeDto>>(types));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<IEnumerable<UserNotificationConfigDto>>> GetSettings()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var settings = await _notificationService.GetUserSettingsAsync(userId);
        return Ok(_mapper.Map<IEnumerable<UserNotificationConfigDto>>(settings));
    }

    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings(UpdateNotificationSettingsDto dto)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var configurations = _mapper.Map<IEnumerable<UserNotificationConfiguration>>(dto.Configurations);
        await _notificationService.UpdateUserSettingsAsync(userId, configurations);
        return Ok();
    }
}