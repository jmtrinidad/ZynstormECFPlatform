using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Data.Services;

public class ClientService(
    IRepository<ApiKey> apiKeyRepository,
    StorageContext context,
    ISqlGenerator sqlGenerator,
    IEmailService emailService) : Repository<Client>(context, sqlGenerator), IClientService
{
    private readonly IRepository<ApiKey> _apiKeyRepository = apiKeyRepository;

    public override async Task<Client?> InsertAsync(Client model)
    {
        var client = await base.InsertAsync(model).ConfigureAwait(false);

        if (client != null && !string.IsNullOrEmpty(client.Email))
        {
            var apiKey = KeyGenerator.GenerateApiKey();
            var secretKey = KeyGenerator.GenerateSecretKey();

            var apiKeyEntity = new ApiKey
            {
                ClientId = client.ClientId,
                Apikey = apiKey,
                SecretKey = secretKey,
                StatusId = (int)StatusEnum.Active,
                RegisteredAt = DateTime.UtcNow,
                GuidId = Guid.NewGuid().ToString()
            };

            await _apiKeyRepository.InsertAsync(apiKeyEntity).ConfigureAwait(false);

            await emailService.SendApiKeyEmailAsync(client.Email, apiKey, secretKey).ConfigureAwait(false);
        }

        return client;
    }
}