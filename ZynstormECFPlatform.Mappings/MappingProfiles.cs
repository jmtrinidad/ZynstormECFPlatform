using AutoMapper;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using Enums = ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Mappings;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // ApiKey
        CreateMap<ApiKeyViewDto, ApiKey>();

        // Client
        CreateMap<ClientCreateDto, Client>()
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => (int)Enums.StatusEnum.Active));

        CreateMap<ClientUpdateDto, Client>()
            .ForMember(dest => dest.ClientId, opt => opt.Ignore());
        // Plan
        CreateMap<PlanOverageTierDto, PlanOverageTier>();
        CreateMap<PlanOverageTier, PlanOverageTierDto>();

        CreateMap<PlanCreateDto, Plan>()
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.IsActive ? (int)Enums.StatusEnum.Active : (int)Enums.StatusEnum.Inactive));

        CreateMap<PlanUpdateDto, Plan>()
            .ForMember(dest => dest.PlanId, opt => opt.Ignore())
            .ForMember(dest => dest.OverageTiers, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.IsActive ? (int)Enums.StatusEnum.Active : (int)Enums.StatusEnum.Inactive));

        CreateMap<Plan, PlanViewDto>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.StatusId == (int)Enums.StatusEnum.Active))
            .ForMember(dest => dest.ClientsCount, opt => opt.MapFrom(src => src.Clients.Count))
            .ForMember(dest => dest.OverageTiers, opt => opt.MapFrom(src => src.OverageTiers.OrderBy(t => t.FromUnit)));

        CreateMap<Client, ClientViewDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Name : null))
            .ForMember(dest => dest.PlanMonthlyFee, opt => opt.MapFrom(src => src.Plan != null ? (decimal?)src.Plan.MonthlyFee : null))
            .ForMember(dest => dest.PlanTypeId, opt => opt.MapFrom(src => src.Plan != null ? (int?)src.Plan.PlanTypeId : null))
            .ForMember(dest => dest.MaxUsers, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.MaxUsers : null))
            .ForMember(dest => dest.ApiKey, opt => opt.MapFrom(src => 
                src.ApiKeys.Where(k => k.StatusId == (int)Enums.StatusEnum.Active)
                           .Select(k => k.Apikey)
                           .FirstOrDefault()
            ));

        // ClientCallBack
        CreateMap<ClientCallBackCreateDto, ClientCallBack>();
        CreateMap<ClientCallBackUpdateDto, ClientCallBack>();
        CreateMap<ClientCallBack, ClientCallBackViewDto>();

        // ClientBranche
        CreateMap<ClientBrancheCreateDto, ClientBranche>();
        CreateMap<ClientBrancheUpdateDto, ClientBranche>();
        CreateMap<ClientBranche, ClientBrancheViewDto>();

        // ClientCertificate
        CreateMap<ClientCertificateCreateDto, ClientCertificate>();
        CreateMap<ClientCertificate, ClientCertificateViewDto>();

        // Currency
        CreateMap<CurrencyCreateDto, Currency>();
        CreateMap<CurrencyUpdateDto, Currency>();
        CreateMap<Currency, CurrencyViewDto>();

        // DGIIUnit
        CreateMap<DGIIUnitCreateDto, DGIIUnit>();
        CreateMap<DGIIUnitUpdateDto, DGIIUnit>();
        CreateMap<DGIIUnit, DGIIUnitViewDto>();

        // EcfStatus
        CreateMap<EcfStatus, EcfStatusViewDto>();

        // EcfStatusHistory
        CreateMap<EcfStatusHistory, EcfStatusHistoryViewDto>();

        // EcfType
        CreateMap<EcfType, EcfTypeViewDto>();

        // SystemLog
        CreateMap<SystemLog, SystemLogViewDto>();

        // User
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>();
        CreateMap<User, UserViewDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

        CreateMap<NotificationType, NotificationTypeDto>();
        CreateMap<UserNotificationConfiguration, UserNotificationConfigDto>().ReverseMap();
    }
}