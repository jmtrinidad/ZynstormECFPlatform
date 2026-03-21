using AutoMapper;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Mappings;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // EcfDocument
        CreateMap<EcfDocumentCreateDto, EcfDocument>()
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.EcfDocumentDetails, opt => opt.MapFrom(src => src.Details))
            .ForMember(dest => dest.EcfDocumentTotals, opt => opt.MapFrom(src => src.Totals));

        CreateMap<EcfDocumentUpdateDto, EcfDocument>()
            .ForMember(dest => dest.EcfDocumentDetails, opt => opt.MapFrom(src => src.Details))
            .ForMember(dest => dest.EcfDocumentTotals, opt => opt.MapFrom(src => src.Totals));

        CreateMap<EcfDocument, EcfDocumentViewDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.EcfStatus))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.EcfType))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency));

        // Details & Totals
        CreateMap<EcfDocumentDetailDto, EcfDocumentDetail>().ReverseMap();
        CreateMap<EcfDocumentTotalDto, EcfDocumentTotal>().ReverseMap();

        // Client
        CreateMap<ClientDto, Client>().ReverseMap();
        CreateMap<ClientBrancheDto, ClientBranche>().ReverseMap();

        // Common
        CreateMap<Status, StatusDto>().ReverseMap();
        CreateMap<EcfStatus, EcfStatusDto>().ReverseMap();
        CreateMap<EcfType, EcfTypeDto>().ReverseMap();
        CreateMap<Currency, CurrencyDto>().ReverseMap();
    }
}