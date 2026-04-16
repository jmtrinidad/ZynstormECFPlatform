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

        CreateMap<ClientUpdateDto, Client>();
        CreateMap<Client, ClientViewDto>();

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
    }
}