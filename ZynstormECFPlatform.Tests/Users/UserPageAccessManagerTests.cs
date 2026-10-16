using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Tests.Users;

public class UserPageAccessManagerTests
{
    private readonly Mock<IUserPageAccessService> _repository = new();
    private readonly Mock<IAccountService> _accounts = new();
    private readonly List<UserPageAccess> _stored = [];
    private List<UserPageAccess>? _inserted;
    private readonly User _user = new() { Id = "u1", FirstName = "Ana", LastName = "Pérez" };

    private UserPageAccessManager CreateManager()
    {
        _repository
            .Setup(r => r.GetManyByAsync(It.IsAny<Expression<Func<UserPageAccess, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _stored.ToList());

        _repository
            .Setup(r => r.InsertAsync(It.IsAny<IEnumerable<UserPageAccess>>()))
            .Returns((IEnumerable<UserPageAccess> rows) =>
            {
                _inserted = rows.ToList();
                return Task.CompletedTask;
            });

        _accounts
            .Setup(a => a.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        return new UserPageAccessManager(_repository.Object, _accounts.Object);
    }

    [Fact]
    public async Task Get_returns_null_when_never_configured()
    {
        _stored.Add(new UserPageAccess { UserId = "u1", PagePath = "/clientes" });

        var result = await CreateManager().GetAsync(_user, "Normal");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("SA")]
    [InlineData("Admin")]
    public async Task Get_returns_null_for_unrestricted_roles(string role)
    {
        _user.PageAccessConfigured = true;
        _stored.Add(new UserPageAccess { UserId = "u1", PagePath = "/clientes" });

        var result = await CreateManager().GetAsync(_user, role);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_returns_stored_paths_once_configured()
    {
        _user.PageAccessConfigured = true;
        _stored.Add(new UserPageAccess { UserId = "u1", PagePath = "/clientes" });

        var result = await CreateManager().GetAsync(_user, "Normal");

        Assert.Equal(["/clientes"], result);
    }

    [Fact]
    public async Task Get_returns_empty_list_when_configured_without_pages()
    {
        _user.PageAccessConfigured = true;

        var result = await CreateManager().GetAsync(_user, "Normal");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task HasAccess_is_true_when_never_configured()
    {
        var result = await CreateManager().HasAccessAsync(_user, "Normal", UserPageAccessManager.DashboardRecentActivity);

        Assert.True(result);
    }

    [Fact]
    public async Task HasAccess_is_true_for_unrestricted_roles_even_if_configured()
    {
        _user.PageAccessConfigured = true;

        var result = await CreateManager().HasAccessAsync(_user, "Admin", UserPageAccessManager.DashboardRecentActivity);

        Assert.True(result);
    }

    [Theory]
    [InlineData(UserPageAccessManager.DashboardRecentActivity, true)]
    [InlineData("/facturas", false)]
    public async Task HasAccess_checks_stored_keys_once_configured(string stored, bool expected)
    {
        _user.PageAccessConfigured = true;
        _stored.Add(new UserPageAccess { UserId = "u1", PagePath = stored });

        var result = await CreateManager().HasAccessAsync(_user, "Normal", UserPageAccessManager.DashboardRecentActivity);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Save_replaces_rows_normalizes_and_marks_configured()
    {
        var old = new UserPageAccess { UserId = "u1", PagePath = "/facturas" };
        _stored.Add(old);

        await CreateManager().SaveAsync(_user, "Normal", [" /clientes ", "/clientes", "", "/CLIENTES", "/facturas"], " /clientes ");

        _repository.Verify(r => r.HardDeleteAsync(It.Is<IEnumerable<UserPageAccess>>(rows => rows.Single() == old)), Times.Once);
        Assert.Equal(["/clientes", "/facturas"], _inserted!.Select(r => r.PagePath));
        Assert.All(_inserted!, r => Assert.Equal("u1", r.UserId));
        Assert.True(_user.PageAccessConfigured);
        Assert.Equal("/clientes", _user.HomePagePath);
        _accounts.Verify(a => a.UpdateUserAsync(_user), Times.Once);
    }

    [Fact]
    public async Task Save_with_no_pages_still_marks_configured()
    {
        await CreateManager().SaveAsync(_user, "Normal", [], null);

        _repository.Verify(r => r.InsertAsync(It.IsAny<IEnumerable<UserPageAccess>>()), Times.Never);
        Assert.True(_user.PageAccessConfigured);
        Assert.Null(_user.HomePagePath);
    }

    [Fact]
    public async Task Save_for_admin_stores_no_rows_and_stays_unrestricted()
    {
        _user.PageAccessConfigured = true;
        _stored.Add(new UserPageAccess { UserId = "u1", PagePath = "/clientes" });

        await CreateManager().SaveAsync(_user, "Admin", ["/clientes"], "");

        _repository.Verify(r => r.HardDeleteAsync(It.IsAny<IEnumerable<UserPageAccess>>()), Times.Once);
        _repository.Verify(r => r.InsertAsync(It.IsAny<IEnumerable<UserPageAccess>>()), Times.Never);
        Assert.False(_user.PageAccessConfigured);
        Assert.Null(_user.HomePagePath);
    }
}
