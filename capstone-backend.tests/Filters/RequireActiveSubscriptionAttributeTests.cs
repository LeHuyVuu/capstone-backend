using capstone_backend.Api.Filters;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace capstone_backend.tests.Filters;

public class RequireActiveSubscriptionAttributeTests
{
    private MyDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MyDbContext(options);
    }

    private ActionExecutingContext CreateActionExecutingContext(
        MyDbContext dbContext,
        ClaimsPrincipal? user = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            User = user ?? new ClaimsPrincipal()
        };
        httpContext.Items["TraceId"] = "TEST-TRACE-ID";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private ClaimsPrincipal CreateUserPrincipal(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task Should_Return_401_When_User_Not_Authenticated()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var filter = new RequireActiveSubscriptionAttribute();
        var context = CreateActionExecutingContext(dbContext);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_UserId_Claim_Missing()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var filter = new RequireActiveSubscriptionAttribute();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Member")
        }, "TestAuth"));
        var context = CreateActionExecutingContext(dbContext, user);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Role_Claim_Missing()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var filter = new RequireActiveSubscriptionAttribute();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "123")
        }, "TestAuth"));
        var context = CreateActionExecutingContext(dbContext, user);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Role_Is_Not_Member_Or_VenueOwner()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var filter = new RequireActiveSubscriptionAttribute();
        var user = CreateUserPrincipal(1, "Admin");
        var context = CreateActionExecutingContext(dbContext, user);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Member_Profile_Not_Found()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        // Create user but no profile
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Member_Profile_Is_Deleted()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = true
        };
        dbContext.MemberProfiles.Add(memberProfile);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Member_Has_No_Valid_Subscription()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Allow_Access_When_Member_Has_Active_Subscription()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Basic",
            Type = "MEMBER",
            Price = 99000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new MemberSubscriptionPackage
        {
            Id = 1,
            MemberId = 1,
            PackageId = 1,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25)
        };
        dbContext.MemberSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        var nextCalled = false;
        
        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context, new List<IFilterMetadata>(), new object()));
        });

        // Assert
        Assert.Null(context.Result); // No error result
        Assert.True(nextCalled); // Next delegate was called
    }

    [Fact]
    public async Task Should_Return_403_When_Member_Subscription_Is_Expired()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Basic",
            Type = "MEMBER",
            Price = 99000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new MemberSubscriptionPackage
        {
            Id = 1,
            MemberId = 1,
            PackageId = 1,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-40),
            EndDate = DateTime.UtcNow.AddDays(-10) // Expired 10 days ago
        };
        dbContext.MemberSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Allow_Access_When_Subscription_Expired_But_Within_Grace_Period()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Basic",
            Type = "MEMBER",
            Price = 99000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new MemberSubscriptionPackage
        {
            Id = 1,
            MemberId = 1,
            PackageId = 1,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-35),
            EndDate = DateTime.UtcNow.AddDays(-5) // Expired 5 days ago
        };
        dbContext.MemberSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute
        {
            GracePeriodDays = 7 // 7-day grace period
        };
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        var nextCalled = false;
        
        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context, new List<IFilterMetadata>(), new object()));
        });

        // Assert
        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Should_Return_403_When_VenueOwner_Profile_Not_Found()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "VenueOwner"
        };
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "VenueOwner");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_VenueOwner_Has_No_Venues()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "VenueOwner"
        };
        dbContext.UserAccounts.Add(user);

        var venueOwnerProfile = new VenueOwnerProfile
        {
            Id = 1,
            UserId = 1,
            BusinessName = "Test Business",
            IsDeleted = false
        };
        dbContext.VenueOwnerProfiles.Add(venueOwnerProfile);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "VenueOwner");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Allow_Access_When_VenueOwner_Has_Active_Subscription()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "VenueOwner"
        };
        dbContext.UserAccounts.Add(user);

        var venueOwnerProfile = new VenueOwnerProfile
        {
            Id = 1,
            UserId = 1,
            BusinessName = "Test Business",
            IsDeleted = false
        };
        dbContext.VenueOwnerProfiles.Add(venueOwnerProfile);

        var venue = new VenueLocation
        {
            Id = 1,
            VenueOwnerId = 1,
            Name = "Test Venue",
            Address = "123 Test St",
            IsDeleted = false
        };
        dbContext.VenueLocations.Add(venue);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Venue Basic",
            Type = "VENUE",
            Price = 299000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new VenueSubscriptionPackage
        {
            Id = 1,
            VenueId = 1,
            PackageId = 1,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25)
        };
        dbContext.VenueSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "VenueOwner");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        var nextCalled = false;
        
        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context, new List<IFilterMetadata>(), new object()));
        });

        // Assert
        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Should_Use_Custom_Error_Message_When_Configured()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute
        {
            CustomErrorMessage = "Premium subscription required for this feature"
        };
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
        
        var responseValue = result.Value;
        var messageProperty = responseValue?.GetType().GetProperty("message");
        var message = messageProperty?.GetValue(responseValue)?.ToString();
        Assert.Equal("Premium subscription required for this feature", message);
    }

    [Fact]
    public async Task Should_Ignore_Subscription_With_Null_StartDate()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Basic",
            Type = "MEMBER",
            Price = 99000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new MemberSubscriptionPackage
        {
            Id = 1,
            MemberId = 1,
            PackageId = 1,
            Status = "Active",
            StartDate = null, // Null StartDate
            EndDate = DateTime.UtcNow.AddDays(25)
        };
        dbContext.MemberSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new object())));

        // Assert - Should fail because subscription has null StartDate
        var result = context.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Should_Check_Status_Case_Insensitively()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        
        var user = new UserAccount
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Member"
        };
        dbContext.UserAccounts.Add(user);

        var memberProfile = new MemberProfile
        {
            Id = 1,
            UserId = 1,
            FullName = "Test Member",
            IsDeleted = false
        };
        dbContext.MemberProfiles.Add(memberProfile);

        var package = new SubscriptionPackage
        {
            Id = 1,
            PackageName = "Basic",
            Type = "MEMBER",
            Price = 99000,
            DurationDays = 30
        };
        dbContext.SubscriptionPackages.Add(package);

        var subscription = new MemberSubscriptionPackage
        {
            Id = 1,
            MemberId = 1,
            PackageId = 1,
            Status = "ACTIVE", // Uppercase
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25)
        };
        dbContext.MemberSubscriptionPackages.Add(subscription);
        await dbContext.SaveChangesAsync();

        var filter = new RequireActiveSubscriptionAttribute();
        var userPrincipal = CreateUserPrincipal(1, "Member");
        var context = CreateActionExecutingContext(dbContext, userPrincipal);
        
        var nextCalled = false;
        
        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context, new List<IFilterMetadata>(), new object()));
        });

        // Assert - Should pass because "ACTIVE" matches "active" case-insensitively
        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }
}
