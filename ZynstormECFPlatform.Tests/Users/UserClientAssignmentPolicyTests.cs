using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Tests.Users;

public class UserClientAssignmentPolicyTests
{
    [Fact]
    public void SA_AllowedIsNull_NothingIsForbidden() =>
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([1, 2, 3], allowed: null));

    [Fact]
    public void Admin_CannotAssignClientsHeDoesNotSee()
    {
        var forbidden = UserClientAssignmentPolicy.FindForbidden([1, 2, 9], new HashSet<int> { 1, 2 });

        Assert.Equal([9], forbidden);
    }

    [Fact]
    public void Admin_CanAssignAnySubsetOfHisClients() =>
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([2], new HashSet<int> { 1, 2 }));

    [Fact]
    public void NoRequestedClients_IsAlwaysValid()
    {
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden(null, new HashSet<int> { 1 }));
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([], new HashSet<int> { 1 }));
    }

    [Fact]
    public void DuplicatedRequestedIds_AreReportedOnce() =>
        Assert.Equal([9], UserClientAssignmentPolicy.FindForbidden([9, 9], new HashSet<int> { 1 }));

    [Fact]
    public void SA_ReplacesAllExistingAssignments() =>
        Assert.Equal([1, 2, 3], UserClientAssignmentPolicy.ClientIdsToReplace([1, 2, 3], allowed: null));

    [Fact]
    public void Admin_ReplacesOnlyTheAssignmentsHeSees_AndKeepsTheOnesAnotherPersonGave() =>
        Assert.Equal([1, 2], UserClientAssignmentPolicy.ClientIdsToReplace([1, 2, 77], new HashSet<int> { 1, 2, 3 }));
}
