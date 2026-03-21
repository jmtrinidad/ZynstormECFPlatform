using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class ClientSupportService : IClientSupportService
{
    private readonly ZynstormEcfPlatformContext _context;

    public ClientSupportService(ZynstormEcfPlatformContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientCallBackDto>> GetCallBacksByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ClientCallBacks
            .Where(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return items.Select(x => new ClientCallBackDto
        {
            ClientCallBackId = x.ClientCallBackId,
            ClientId = x.ClientId,
            CallBackUrl = x.CallBackUrl,
            Description = x.Description,
            IsActive = x.IsActive
        });
    }

    public async Task<ClientCallBackDto> CreateCallBackAsync(ClientCallBackDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ClientCallBack
        {
            ClientId = dto.ClientId,
            CallBackUrl = dto.CallBackUrl,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.ClientCallBacks.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        dto.ClientCallBackId = entity.ClientCallBackId;
        return dto;
    }

    public async Task<IEnumerable<ClientCertificateDto>> GetCertificatesByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ClientCertificates
            .Where(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return items.Select(x => new ClientCertificateDto
        {
            ClientCertificateId = x.ClientCertificateId,
            ClientId = x.ClientId,
            CertificatePath = x.CertificatePath,
            Password = x.Password,
            ExpiryDate = x.ExpiryDate
        });
    }

    public async Task<ClientCertificateDto> CreateCertificateAsync(ClientCertificateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ClientCertificate
        {
            ClientId = dto.ClientId,
            CertificatePath = dto.CertificatePath,
            Password = dto.Password,
            ExpiryDate = dto.ExpiryDate
        };

        _context.ClientCertificates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        dto.ClientCertificateId = entity.ClientCertificateId;
        return dto;
    }
}
