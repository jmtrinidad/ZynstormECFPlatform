using AutoMapper;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    public class ClientController(
       IClientService clientService,
        IMapper mapper,
        ILoggerFactory loggerFactory) : BaseController<ClientController, Client, ClientCreateDto, ClientUpdateDto, ClientViewDto>(clientService, mapper, loggerFactory)
    {
        /*

        {
          "name": "TRASPORTE NJ",
          "rnc": "133-00988-9",
          "email": "JM.TRINIDAD.99@HOTMAIL.COM",
          "phone": "809-876-4046",
          "statusId": 1
        }

         */
    }
}