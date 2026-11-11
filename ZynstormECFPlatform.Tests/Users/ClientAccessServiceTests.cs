using System.Linq.Expressions;
using System.Security.Claims;
using Moq;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Tests.Users;

public class ClientAccessServiceTests
{
    private readonly List<UserClient> _links = [];
    private readonly List<Client> _clients = [];
    private readonly Mock<IUserClientService> _userClients = new();
    private readonly Mock<IClientService> _clientService = new();

    public ClientAccessServiceTests()
    {
        // Los predicados reales se evalúan contra las listas, así la prueba comprueba la regla y no el mock.
        _userClients
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<UserClient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<UserClient, bool>> p, CancellationToken _) => _links.FirstOrDefault(p.Compile()));

        _userClients
            .Setup(s => s.GetManyByAsync(It.IsAny<Expression<Func<UserClient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<UserClient, bool>> p, CancellationToken _) =>
                (IEnumerable<UserClient>)_links.Where(p.Compile()).ToList());

        _clientService
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<Client, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Client, bool>> p, CancellationToken _) => _clients.FirstOrDefault(p.Compile()));
    }

    private ClientAccessService CreateService() => new(_userClients.Object, _clientService.Object);

    private static ClaimsPrincipal User(string role, string? userId = "u1")
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task SA_CanAccessAnyClient_WithoutQueryingAssignments()
    {
        var allowed = await CreateService().CanAccessClientAsync(User("SA"), 99);

        Assert.True(allowed);
        _userClients.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_CanAccessOnlyAssignedClients()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        var service = CreateService();

        Assert.True(await service.CanAccessClientAsync(User("Admin"), 5));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), 6));
    }

    [Fact]
    public async Task Admin_DoesNotInheritAnotherUsersAssignments()
    {
        _links.Add(new UserClient { UserId = "other", ClientId = 5 });

        Assert.False(await CreateService().CanAccessClientAsync(User("Admin"), 5));
    }

    [Fact]
    public async Task UserWithoutId_HasNoAccess()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });

        Assert.False(await CreateService().CanAccessClientAsync(User("Admin", userId: null), 5));
    }

    [Fact]
    public async Task ByGuid_ResolvesTheClientAndChecksTheAssignment()
    {
        _clients.Add(new Client { ClientId = 5, GuidId = "g-5" });
        _clients.Add(new Client { ClientId = 6, GuidId = "g-6" });
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        var service = CreateService();

        Assert.True(await service.CanAccessClientAsync(User("Admin"), "g-5"));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), "g-6"));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), "missing"));
    }

    [Fact]
    public async Task ByGuid_SA_IsAllowedWithoutLookingTheClientUp()
    {
        Assert.True(await CreateService().CanAccessClientAsync(User("SA"), "anything"));
        _clientService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AccessibleClientIds_AreNullForSA_AndTheAssignedOnesOtherwise()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        _links.Add(new UserClient { UserId = "u1", ClientId = 7 });
        _links.Add(new UserClient { UserId = "other", ClientId = 9 });
        var service = CreateService();

        Assert.Null(await service.GetAccessibleClientIdsAsync(User("SA")));
        Assert.Equal(new HashSet<int> { 5, 7 }, await service.GetAccessibleClientIdsAsync(User("Admin")));
    }
}
