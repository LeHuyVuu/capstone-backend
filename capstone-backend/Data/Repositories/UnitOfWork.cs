using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Unit of Work implementation for coordinating repository operations
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MyDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(MyDbContext context, 
        IUserRepository userRepository, 
        IMemberProfileRepository memberProfileRepository,
        IMemberMoodLogRepository memberMoodLogRepository,
        IMoodTypeRepository moodTypeRepository,
        ICoupleProfileRepository coupleProfileRepository,
        ICoupleInvitationRepository coupleInvitationRepository,
        ITestTypeRepository testTypeRepository,
        IQuestionRepository questionRepository,
        IQuestionAnswerRepository questionAnswerRepository,
        IPersonalityTestRepository personalityTestRepository,
        IVenueLocationRepository venueLocationRepository,
        ILocationTagRepository locationTagRepository,
        IDatePlanRepository datePlanRepository,
        IDatePlanItemRepository datePlanItemRepository,
        IVenueOwnerProfileRepository venueOwnerProfileRepository,
        INotificationRepository notificationRepository,
        IDeviceTokenRepository deviceTokenRepository,
        IReviewRepository reviewRepository)
    {
        _context = context;
        Users = userRepository;
        MembersProfile = memberProfileRepository;
        MemberMoodLogs = memberMoodLogRepository;
        MoodTypes = moodTypeRepository;
        CoupleProfiles = coupleProfileRepository;
        CoupleInvitations = coupleInvitationRepository;
        TestTypes = testTypeRepository;
        Questions = questionRepository;
        QuestionAnswers = questionAnswerRepository;
        PersonalityTests = personalityTestRepository;
        VenueLocations = venueLocationRepository;
        LocationTags = locationTagRepository;
        DatePlans = datePlanRepository;
        DatePlanItems = datePlanItemRepository;
        VenueOwnerProfiles = venueOwnerProfileRepository;
        Notifications = notificationRepository;
        DeviceTokens = deviceTokenRepository;
        Reviews = reviewRepository;
    }

    public MyDbContext Context => _context;

    public IUserRepository Users { get; }

    public IMemberProfileRepository MembersProfile { get; }

    public IMemberMoodLogRepository MemberMoodLogs { get; }

    public IMoodTypeRepository MoodTypes { get; }

    public ICoupleProfileRepository CoupleProfiles { get; }

    public ICoupleInvitationRepository CoupleInvitations { get; }

    public ITestTypeRepository TestTypes { get; }

    public IPersonalityTestRepository PersonalityTests { get; }

    public IQuestionRepository Questions { get; }

    public IQuestionAnswerRepository QuestionAnswers { get; }

    public IVenueLocationRepository VenueLocations { get; }

    public ILocationTagRepository LocationTags { get; }

    public IDatePlanRepository DatePlans { get; }

    public IDatePlanItemRepository DatePlanItems { get; }

    public IVenueOwnerProfileRepository VenueOwnerProfiles { get; }

    public INotificationRepository Notifications { get; }

    public IDeviceTokenRepository DeviceTokens { get; }

    public IReviewRepository Reviews { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
