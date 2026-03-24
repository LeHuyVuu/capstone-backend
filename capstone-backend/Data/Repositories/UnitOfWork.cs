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
        IDatePlanJobRepository datePlanJobRepository,
        IReviewRepository reviewRepository,
        ICheckInHistoryRepository checkInHistoryRepository,
        IMediaRepository mediaRepository,
        IReviewReplyRepository reviewReplyRepository,
        IReviewLikeRepository reviewLikeRepository,
        IChallengeRepository challengeRepository,
        IAdvertisementRepository advertisementRepository,
        ISpecialEventRepository specialEventRepository,
        IPostRepository postRepository,
        IPostLikeRepository postLikeRepository,
        ICommentRepository commentRepository,
        ICommentLikeRepository commentLikeRepository,
        ICoupleProfileChallengeRepository coupleProfileChallengeRepository,
        ICouplePersonalityTypeRepository couplePersonalityTypeRepository,
        IVoucherRepository voucherRepository,
        IVoucherItemRepository voucherItemRepository,
        IVoucherItemMemberRepository voucherItemMemberRepository,
        IVoucherLocationRepository voucherLocationRepository,
        ICategoryRepository categoryRepository,
        IVoucherJobRepository voucherJobRepository,
        IVoucherItemJobRepository voucherItemJobRepository,
        IWalletRepository walletRepository,
        IWithdrawRequestRepository withdrawRequestRepository,
        ITransactionRepository transactionRepository,
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IMemberSubscriptionPackageRepository memberSubscriptionPackageRepository,
        ILeaderboardRepository leaderboardRepository,
        IReportRepository reportRepository,
        IReportTypeRepository reportTypeRepository,
        IVenueSettlementRepository venueSettlementRepository,
        ISystemConfigRepository systemConfigRepository,
        IAccessoryRepository accessoryRepository,
        IAccessoryPurchaseRepository accessoryPurchaseRepository,
        IMemberAccessoryRepository memberAccessoryRepository)
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
        DatePlanJobs = datePlanJobRepository;
        Reviews = reviewRepository;
        CheckInHistories = checkInHistoryRepository;
        Media = mediaRepository;
        ReviewReplies = reviewReplyRepository;
        ReviewLikes = reviewLikeRepository;
        Challenges = challengeRepository;
        Advertisements = advertisementRepository;
        SpecialEvents = specialEventRepository;
        Posts = postRepository;
        PostLikes = postLikeRepository;
        Comments = commentRepository;
        CommentLikes = commentLikeRepository;
        CoupleProfileChallenges = coupleProfileChallengeRepository;
        CouplePersonalityTypes = couplePersonalityTypeRepository;
        Vouchers = voucherRepository;
        VoucherItems = voucherItemRepository;
        VoucherItemMembers = voucherItemMemberRepository;
        VoucherLocations = voucherLocationRepository;
        Categories = categoryRepository;
        VoucherJobs = voucherJobRepository;
        VoucherItemJobs = voucherItemJobRepository;
        Wallets = walletRepository;
        WithdrawRequests = withdrawRequestRepository;
        Transactions = transactionRepository;
        SubscriptionPackages = subscriptionPackageRepository;
        MemberSubscriptionPackages = memberSubscriptionPackageRepository;
        Leaderboards = leaderboardRepository;
        Reports = reportRepository;
        ReportTypes = reportTypeRepository;
        VenueSettlements = venueSettlementRepository;
        SystemConfigs = systemConfigRepository;
        Accessories = accessoryRepository;
        AccessoryPurchases = accessoryPurchaseRepository;
        MemberAccessories = memberAccessoryRepository;
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

    public IDatePlanJobRepository DatePlanJobs { get; set; }

    public IReviewRepository Reviews { get; }

    public ICheckInHistoryRepository CheckInHistories { get; }

    public IMediaRepository Media { get; }

    public IReviewReplyRepository ReviewReplies { get; }

    public IReviewLikeRepository ReviewLikes { get; }

    public IChallengeRepository Challenges { get; }

    public IAdvertisementRepository Advertisements { get; }

    public ISpecialEventRepository SpecialEvents { get; }

    public IPostRepository Posts { get; set; }

    public IPostLikeRepository PostLikes { get; set; }

    public ICommentRepository Comments { get; set; }

    public ICommentLikeRepository CommentLikes { get; set; }

    public ICoupleProfileChallengeRepository CoupleProfileChallenges { get; set; }

    public ICouplePersonalityTypeRepository CouplePersonalityTypes { get; set; }

    public IVoucherRepository Vouchers { get; }

    public IVoucherItemRepository VoucherItems { get; }

    public IVoucherItemMemberRepository VoucherItemMembers { get; }

    public IVoucherLocationRepository VoucherLocations { get; }

    public ICategoryRepository Categories { get; }

    public IVoucherJobRepository VoucherJobs { get; set; }

    public IVoucherItemJobRepository VoucherItemJobs { get; set; }

    public IWalletRepository Wallets { get; }

    public IWithdrawRequestRepository WithdrawRequests { get; }

    public ITransactionRepository Transactions { get; set; }

    public ISubscriptionPackageRepository SubscriptionPackages { get; set; }

    public IMemberSubscriptionPackageRepository MemberSubscriptionPackages { get; set; }

    public ILeaderboardRepository Leaderboards { get; }

    public IReportRepository Reports { get; }

    public IReportTypeRepository ReportTypes { get; }

    public IVenueSettlementRepository VenueSettlements { get; }

    public ISystemConfigRepository SystemConfigs { get; }

    public IAccessoryRepository Accessories { get; }

    public IAccessoryPurchaseRepository AccessoryPurchases { get; }

    public IMemberAccessoryRepository MemberAccessories { get; }

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
