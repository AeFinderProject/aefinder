using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AeFinder.User;
using AeFinder.User.Dto;
using AeFinder.Grains.Grain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using Moq;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Xunit;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.Users;

public class BatchDeleteUsersTests
{
    private readonly Mock<IdentityUserManager> _userManager;
    private readonly Mock<IUnitOfWorkManager> _unitOfWorkManager = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserAppService _service;

    public BatchDeleteUsersTests()
    {
        var repository = Mock.Of<IIdentityUserRepository>();
        var roleRepository = Mock.Of<IIdentityRoleRepository>();
        var options = OptionsFactory.Create(new IdentityOptions());
        var store = new IdentityUserStore(repository, roleRepository, null,
            NullLogger<IdentityRoleStore>.Instance, new UpperInvariantLookupNormalizer());
        _userManager = new Mock<IdentityUserManager>(store, roleRepository, repository, options,
            new PasswordHasher<IdentityUser>(), Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(), new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(),
            NullLogger<IdentityUserManager>.Instance, null, null, null, null, null, null);
        _unitOfWorkManager.Setup(x => x.Begin(It.IsAny<AbpUnitOfWorkOptions>(), true))
            .Returns(_unitOfWork.Object);
        var services = new ServiceCollection()
            .AddSingleton(_unitOfWorkManager.Object)
            .AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        _service = new UserAppService(_userManager.Object, repository, roleRepository, null, options,
            null, null, null, null, null, null, null, null,
            Mock.Of<IOptionsSnapshot<EmailTemplateOptions>>(o => o.Value == new EmailTemplateOptions()),
            Mock.Of<IOptionsSnapshot<UserRegisterOptions>>(o => o.Value == new UserRegisterOptions()))
        {
            LazyServiceProvider = new AbpLazyServiceProvider(services)
        };
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(201)]
    public void Reject_Invalid_List_Length(int count)
    {
        var input = new BatchDeleteUsersInput
        {
            UserIds = count < 0 ? null : Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList()
        };
        Validator.TryValidateObject(input, new ValidationContext(input), new List<ValidationResult>(), true)
            .ShouldBeFalse();
    }

    [Fact]
    public void Reject_Empty_Guid()
    {
        var input = new BatchDeleteUsersInput { UserIds = new List<Guid> { Guid.Empty } };
        Validator.TryValidateObject(input, new ValidationContext(input), new List<ValidationResult>(), true)
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_Users_With_And_Without_Organizations_Once()
    {
        var user = new IdentityUser(Guid.NewGuid(), "pending", "pending@example.com");
        var member = new IdentityUser(Guid.NewGuid(), "member", "member@example.com");
        member.OrganizationUnits.Add(new IdentityUserOrganizationUnit(member.Id, Guid.NewGuid()));
        foreach (var item in new[] { user, member })
        {
            _userManager.Setup(x => x.FindByIdAsync(item.Id.ToString())).ReturnsAsync(item);
            _userManager.Setup(x => x.DeleteAsync(item)).ReturnsAsync(IdentityResult.Success);
        }

        var result = await _service.BatchDeleteAsync(new BatchDeleteUsersInput
        {
            UserIds = new List<Guid> { user.Id, member.Id, user.Id }
        });

        result.DeletedUserIds.ShouldBe(new[] { user.Id, member.Id });
        result.SkippedUserIds.ShouldBeEmpty();
        result.FailedUserIds.ShouldBeEmpty();
        _userManager.Verify(x => x.DeleteAsync(user), Times.Once);
        _userManager.Verify(x => x.DeleteAsync(member), Times.Once);
        _unitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkManager.Verify(x => x.Begin(
            It.Is<AbpUnitOfWorkOptions>(o => !o.IsTransactional), true), Times.Exactly(2));
    }

    [Fact]
    public async Task Skip_Missing_And_Already_Deleted_Users()
    {
        var missingId = Guid.NewGuid();
        var deleted = new IdentityUser(Guid.NewGuid(), "deleted", "deleted@example.com") { IsDeleted = true };
        _userManager.Setup(x => x.FindByIdAsync(deleted.Id.ToString())).ReturnsAsync(deleted);

        var result = await _service.BatchDeleteAsync(new BatchDeleteUsersInput
        {
            UserIds = new List<Guid> { missingId, deleted.Id }
        });

        result.SkippedUserIds.ShouldBe(new[] { missingId, deleted.Id });
        result.DeletedUserIds.ShouldBeEmpty();
        result.FailedUserIds.ShouldBeEmpty();
        _userManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Never);
    }

    [Fact]
    public async Task Report_Failures_And_Continue_With_Other_Users()
    {
        var users = Enumerable.Range(0, 4)
            .Select(i => new IdentityUser(Guid.NewGuid(), "user" + i, $"user{i}@example.com")).ToArray();
        foreach (var user in users)
        {
            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        }
        _userManager.Setup(x => x.DeleteAsync(users[0]))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DeleteFailed" }));
        _userManager.Setup(x => x.DeleteAsync(users[1])).ThrowsAsync(new InvalidOperationException("storage failed"));
        _unitOfWork.SetupSequence(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("completion failed"))
            .Returns(Task.CompletedTask);

        var result = await _service.BatchDeleteAsync(new BatchDeleteUsersInput
        {
            UserIds = users.Select(u => u.Id).ToList()
        });

        result.FailedUserIds.ShouldBe(users.Take(3).Select(u => u.Id));
        result.DeletedUserIds.ShouldBe(new[] { users[3].Id });
        result.SkippedUserIds.ShouldBeEmpty();
    }
}
