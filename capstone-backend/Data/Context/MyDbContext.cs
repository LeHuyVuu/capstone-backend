using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accessory> Accessories { get; set; }

    public virtual DbSet<AdsOrder> AdsOrders { get; set; }

    public virtual DbSet<Advertisement> Advertisements { get; set; }

    public virtual DbSet<AdvertisementPackage> AdvertisementPackages { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<BlogLike> BlogLikes { get; set; }

    public virtual DbSet<Challenge> Challenges { get; set; }

    public virtual DbSet<CheckInHistory> CheckInHistories { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<CollectionVenueLocation> CollectionVenueLocations { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentLike> CommentLikes { get; set; }

    public virtual DbSet<CoupleMoodLog> CoupleMoodLogs { get; set; }

    public virtual DbSet<CoupleMoodType> CoupleMoodTypes { get; set; }

    public virtual DbSet<CouplePersonalityType> CouplePersonalityTypes { get; set; }

    public virtual DbSet<CoupleProfile> CoupleProfiles { get; set; }

    public virtual DbSet<CoupleInvitation> CoupleInvitations { get; set; }

    public virtual DbSet<CoupleProfileChallenge> CoupleProfileChallenges { get; set; }

    public virtual DbSet<DatePlan> DatePlans { get; set; }

    public virtual DbSet<DatePlanItem> DatePlanItems { get; set; }

    public virtual DbSet<DatePlanJob> DatePlanJobs { get; set; }

    public virtual DbSet<DeviceToken> DeviceTokens { get; set; }

    public virtual DbSet<Interaction> Interactions { get; set; }

    public virtual DbSet<Leaderboard> Leaderboards { get; set; }

    public virtual DbSet<LocationFollower> LocationFollowers { get; set; }

    public virtual DbSet<LocationTag> LocationTags { get; set; }

    public virtual DbSet<Media> Media { get; set; }

    public virtual DbSet<MemberAccessory> MemberAccessories { get; set; }

    public virtual DbSet<MemberMoodLog> MemberMoodLogs { get; set; }

    public virtual DbSet<MemberProfile> MemberProfiles { get; set; }

    public virtual DbSet<MemberSubscriptionPackage> MemberSubscriptionPackages { get; set; }

    public virtual DbSet<MoodType> MoodTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OwnerMember> OwnerMembers { get; set; }

    public virtual DbSet<PersonalityTest> PersonalityTests { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAnswer> QuestionAnswers { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportType> ReportTypes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ReviewLike> ReviewLikes { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<SpecialEvent> SpecialEvents { get; set; }

    public virtual DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }

    public virtual DbSet<TestType> TestTypes { get; set; }

    public virtual DbSet<TopSearch> TopSearches { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    public virtual DbSet<VenueLocation> VenueLocations { get; set; }

    public virtual DbSet<VenueLocationTag> VenueLocationTags { get; set; }

    public virtual DbSet<VenueOpeningHour> VenueOpeningHours { get; set; }

    public virtual DbSet<VenueLocationAdvertisement> VenueLocationAdvertisements { get; set; }

    public virtual DbSet<VenueOwnerProfile> VenueOwnerProfiles { get; set; }

    public virtual DbSet<VenueSubscriptionPackage> VenueSubscriptionPackages { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherItem> VoucherItems { get; set; }

    public virtual DbSet<VoucherItemMember> VoucherItemMembers { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WithdrawRequest> WithdrawRequests { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    // Messaging entities
    public virtual DbSet<Conversation> Conversations { get; set; }
    public virtual DbSet<ConversationMember> ConversationMembers { get; set; }
    public virtual DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FIX: Convert tất cả DateTime về UTC để tránh lỗi PostgreSQL
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        )
                    );
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
                        )
                    );
                }
            }
        }

        modelBuilder.Entity<Accessory>(entity =>
        {
            entity.ToTable("accessories");
            entity.HasKey(e => e.Id).HasName("accessories_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.ResourceUrl).HasColumnName("resource_url");
            entity.Property(e => e.AvailableQuantity).HasColumnName("available_quantity");
            entity.Property(e => e.AvailableFrom).HasColumnName("available_from");
            entity.Property(e => e.AvailableTo).HasColumnName("available_to");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.IsLimited).HasDefaultValue(false).HasColumnName("is_limited");
            entity.Property(e => e.PricePoint).HasDefaultValue(0).HasColumnName("price_point");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<AdsOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ads_orders_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Advertisement).WithMany(p => p.AdsOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ads_orders_advertisement_id_fkey");

            entity.HasOne(d => d.Package).WithMany(p => p.AdsOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ads_orders_package_id_fkey");
        });

        modelBuilder.Entity<Advertisement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("advertisements_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.VenueOwner).WithMany(p => p.Advertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("advertisements_venue_owner_id_fkey");
        });

        modelBuilder.Entity<AdvertisementPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("advertisement_packages_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PriorityScore).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("blogs_pkey");

            entity.Property(e => e.CommentCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.Blogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("blogs_member_id_fkey");
        });

        modelBuilder.Entity<BlogLike>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("blog_likes_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Blog).WithMany(p => p.BlogLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("blog_likes_blog_id_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.BlogLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("blog_likes_member_id_fkey");
        });

        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("challenges_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.RewardPoints).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<CheckInHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("check_in_histories_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsValid).HasDefaultValue(true);

            entity.HasOne(d => d.Member).WithMany(p => p.CheckInHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_histories_member_id_fkey");

            entity.HasOne(d => d.Venue).WithMany(p => p.CheckInHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_histories_venue_id_fkey");
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("collections_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.Collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("collections_member_id_fkey");

            entity.HasMany(d => d.Venues)
                  .WithMany(p => p.Collections)
                  .UsingEntity<CollectionVenueLocation>();
        });

        modelBuilder.Entity<CollectionVenueLocation>(entity =>
        {

            entity.HasKey(e => e.Id).HasName("collection_venue_locations_pkey");

            entity.HasOne(d => d.Collection)
                  .WithMany() 
                  .HasForeignKey(d => d.CollectionId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("collection_venue_locations_collection_id_fkey");

            entity.HasOne(d => d.Venue)
                  .WithMany()
                  .HasForeignKey(d => d.VenueId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("collection_venue_locations_venue_id_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.Property(e => e.CommentCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Blog).WithMany(p => p.Comments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_blog_id_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.Comments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comments_member_id_fkey");
        });

        modelBuilder.Entity<CommentLike>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comment_likes_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comment_likes_comment_id_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.CommentLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comment_likes_member_id_fkey");
        });

        modelBuilder.Entity<CoupleMoodLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("couple_mood_logs_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Couple).WithMany(p => p.CoupleMoodLogs).HasConstraintName("couple_mood_logs_couple_id_fkey");

            entity.HasOne(d => d.CoupleMoodType).WithMany(p => p.CoupleMoodLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_mood_logs_couple_mood_type_id_fkey");
        });

        modelBuilder.Entity<CoupleMoodType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("couple_mood_types_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<CouplePersonalityType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("couple_personality_types_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<CoupleProfile>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_profiles_pkey");

            entity.Property(e => e.MemberId1).HasColumnName("member_id_1");
            entity.Property(e => e.MemberId2).HasColumnName("member_id_2");
            entity.Property(e => e.BudgetMin).HasDefaultValueSql("0");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.InteractionPoints).HasDefaultValue(0);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.TotalPoints).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CoupleMoodType).WithMany(p => p.CoupleProfiles).HasConstraintName("couple_profiles_couple_mood_type_id_fkey");

            entity.HasOne(d => d.CouplePersonalityType).WithMany(p => p.CoupleProfiles).HasConstraintName("couple_profiles_couple_personality_type_id_fkey");

            entity.HasOne(d => d.MemberId1Navigation).WithMany(p => p.CoupleProfilememberId1Navigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_profiles_member_id_1_fkey");

            entity.HasOne(d => d.MemberId2Navigation).WithMany(p => p.CoupleProfilememberId2Navigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_profiles_member_id_2_fkey");
        });

        modelBuilder.Entity<CoupleInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("couple_invitations_pkey");

            entity.Property(e => e.Status).HasDefaultValue("PENDING");
            entity.Property(e => e.SentAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.SenderMember).WithMany(p => p.CoupleInvitationsSent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_invitations_sender_member_id_fkey");

            entity.HasOne(d => d.ReceiverMember).WithMany(p => p.CoupleInvitationsReceived)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_invitations_receiver_member_id_fkey");
        });

        modelBuilder.Entity<CoupleProfileChallenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("couple_profile_challenges_pkey");

            entity.Property(e => e.CompletedMemberIds).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrentProgress).HasDefaultValue(0);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Challenge).WithMany(p => p.CoupleProfileChallenges).HasConstraintName("couple_profile_challenges_challenge_id_fkey");

            entity.HasOne(d => d.Couple).WithMany(p => p.CoupleProfileChallenges).HasConstraintName("couple_profile_challenges_couple_id_fkey");
        });

        modelBuilder.Entity<DatePlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("date_plans_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EstimatedBudget).HasDefaultValueSql("0");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Couple).WithMany(p => p.DatePlans).HasConstraintName("date_plans_couple_id_fkey");
            entity.HasOne(d => d.OrganizerMember)
                .WithMany(p => p.MemberProfiles)
                .HasForeignKey(d => d.OrganizerMemberId)
                .HasConstraintName("fk_date_plans_organizer_member")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DatePlanItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("date_plan_items_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.DatePlan).WithMany(p => p.DatePlanItems).HasConstraintName("date_plan_items_date_plan_id_fkey");

            entity.HasOne(d => d.VenueLocation).WithMany(p => p.DatePlanItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("date_plan_items_venue_location_id_fkey");
        });

        modelBuilder.Entity<DatePlanJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("date_plan_jobs_pkey");

            entity.HasOne(j => j.DatePlan)
                  .WithMany(dp => dp.DatePlanJobs)
                  .HasConstraintName("fk_date_plan_jobs_date_plans_date_plan_id")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_tokens_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.DeviceTokens).HasConstraintName("device_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Interaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("interactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member)
            .WithMany(p => p.Interactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_interaction_member");
        });

        modelBuilder.Entity<Leaderboard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("leaderboards_pkey");

            entity.Property(e => e.TotalPoints).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Couple).WithMany(p => p.Leaderboards).HasConstraintName("leaderboards_couple_id_fkey");
        });

        modelBuilder.Entity<LocationFollower>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("location_followers_pkey");

            entity.ToTable(tb => tb.HasComment("Bảng quan hệ theo dõi / chia sẻ vị trí giữa users"));

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.FollowerShareStatus).HasDefaultValueSql("'SHARING'::character varying");
            entity.Property(e => e.FollowerUserId).HasComment("User theo dõi");
            entity.Property(e => e.IsMuted).HasDefaultValue(false);
            entity.Property(e => e.OwnerShareStatus).HasDefaultValueSql("'SHARING'::character varying");
            entity.Property(e => e.OwnerUserId).HasComment("User trung tâm");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasComment("ACTIVE, REMOVED, BLOCKED, PENDING");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<LocationTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("location_tags_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CoupleMoodType).WithMany(p => p.LocationTags).HasConstraintName("location_tags_couple_mood_type_id_fkey");

            entity.HasOne(d => d.CouplePersonalityType).WithMany(p => p.LocationTags).HasConstraintName("location_tags_couple_personality_type_id_fkey");
        });

        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("media_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<MemberAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_accessories_pkey");

            entity.Property(e => e.AcquiredAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsEquipped).HasDefaultValue(false);

            entity.HasOne(d => d.Accessory).WithMany(p => p.MemberAccessories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("member_accessories_accessory_id_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberAccessories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("member_accessories_member_id_fkey");
        });

        modelBuilder.Entity<MemberMoodLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_mood_logs_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsPrivate).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberMoodLogs).HasConstraintName("member_mood_logs_member_id_fkey");

            entity.HasOne(d => d.MoodType).WithMany(p => p.MemberMoodLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_mood_logs_mood_type_id_fkey");
        });

        modelBuilder.Entity<MemberProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_profiles_pkey");

            entity.Property(e => e.BudgetMin).HasDefaultValueSql("0");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.RelationshipStatus).HasDefaultValueSql("'SINGLE'::text");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.MoodTypes).WithMany(p => p.MemberProfiles).HasConstraintName("member_profiles_mood_types_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.MemberProfiles).HasConstraintName("member_profiles_user_id_fkey");
        });

        modelBuilder.Entity<MemberSubscriptionPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_subscription_packages_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberSubscriptionPackages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_subscription_packages_member_id_fkey");

            entity.HasOne(d => d.Package).WithMany(p => p.MemberSubscriptionPackages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_subscription_packages_package_id_fkey");
        });

        modelBuilder.Entity<MoodType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mood_types_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.HasIndex(e => e.UserId, "idx_noti_user_unread").HasFilter("(is_read = false)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<OwnerMember>(entity =>
        {
            entity.HasKey(e => new { e.OwnerUserId, e.MemberUserId }).HasName("owner_members_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<PersonalityTest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("personality_tests_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.PersonalityTests).HasConstraintName("personality_tests_member_id_fkey");

            entity.HasOne(d => d.TestType).WithMany(p => p.PersonalityTests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("personality_tests_test_type_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("questions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.TestType).WithMany(p => p.Questions).HasConstraintName("questions_test_type_id_fkey");
        });

        modelBuilder.Entity<QuestionAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("question_answers_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);
            entity.Property(e => e.ScoreValue).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionAnswers).HasConstraintName("question_answers_question_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reports_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Reporter).WithMany(p => p.Reports).HasConstraintName("reports_reporter_id_fkey");
        });

        modelBuilder.Entity<ReportType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("report_types_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reviews_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reviews_member_id_fkey");

            entity.HasOne(d => d.Venue).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reviews_venue_id_fkey");
        });

        modelBuilder.Entity<ReviewLike>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("review_likes_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.ReviewLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("review_likes_member_id_fkey");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewLikes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("review_likes_review_id_fkey");
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("search_histories_pkey");

            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.ResultCount).HasDefaultValue(0);
            entity.Property(e => e.SearchedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.SearchHistories)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("search_histories_member_id_fkey");
        });

        modelBuilder.Entity<SpecialEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("special_events_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<SubscriptionPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_packages_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Price).HasDefaultValueSql("0");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<TestType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("test_types_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.TotalQuestions).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<TopSearch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("top_searches_pkey");

            entity.Property(e => e.HitCount).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_accounts_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");
            entity.Property(e => e.Role).HasDefaultValueSql("'MEMBER'::text").HasColumnName("role");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<VenueLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_locations_pkey");

            entity.Property(e => e.AvarageCost).HasDefaultValueSql("0");
            entity.Property(e => e.AverageRating).HasDefaultValueSql("0");
            entity.Property(e => e.CoverImage).HasColumnName("cover_image");
            entity.Property(e => e.InteriorImage).HasColumnName("interior_image");
            entity.Property(e => e.FullPageMenuImage).HasColumnName("full_page_menu_image");
            entity.Property(e => e.IsOwnerVerified).HasDefaultValue(false).HasColumnName("is_owner_verified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PriceMin).HasDefaultValueSql("0");
            entity.Property(e => e.ReviewCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.VenueOwner).WithMany(p => p.VenueLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_locations_venue_owner_id_fkey");
        });

        modelBuilder.Entity<VenueLocationTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_location_tags_pkey");

            entity.ToTable("venue_location_tags");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VenueLocationId).HasColumnName("venue_location_id");
            entity.Property(e => e.LocationTagId).HasColumnName("location_tag_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");

            entity.HasOne(d => d.VenueLocation)
                .WithMany(p => p.VenueLocationTags)
                .HasForeignKey(d => d.VenueLocationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_venue_location_tags_venue_location");

            entity.HasOne(d => d.LocationTag)
                .WithMany(p => p.VenueLocationTags)
                .HasForeignKey(d => d.LocationTagId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_venue_location_tags_location_tag");

            entity.HasIndex(e => new { e.VenueLocationId, e.LocationTagId })
                .IsUnique()
                .HasDatabaseName("idx_venue_location_tag_unique");
        });

        modelBuilder.Entity<VenueOpeningHour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_opening_hours_pkey");

            entity.Property(e => e.IsClosed).HasDefaultValue(false);

            entity.HasOne(d => d.VenueLocation)
                .WithMany(p => p.VenueOpeningHours)
                .HasForeignKey(d => d.VenueLocationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_venue_opening_hours_venue_locations");
        });

        modelBuilder.Entity<VenueLocationAdvertisement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_location_advertisements_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PriorityScore).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Advertisement).WithMany(p => p.VenueLocationAdvertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_location_advertisements_advertisement_id_fkey");

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueLocationAdvertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_location_advertisements_venue_id_fkey");
        });

        modelBuilder.Entity<VenueOwnerProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_owner_profiles_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.VenueOwnerProfiles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_owner_profiles_user_id_fkey");
        });

        modelBuilder.Entity<VenueSubscriptionPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venue_subscription_packages_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Package).WithMany(p => p.VenueSubscriptionPackages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_subscription_packages_package_id_fkey");

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueSubscriptionPackages).HasConstraintName("venue_subscription_packages_venue_id_fkey");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vouchers_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UsageLimitPerMember).HasDefaultValue(1);

            entity.HasOne(d => d.VenueOwner).WithMany(p => p.Vouchers).HasConstraintName("vouchers_venue_owner_id_fkey");
        });

        modelBuilder.Entity<VoucherItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("voucher_items_pkey");

            entity.Property(e => e.AcquiredAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("voucher_items_voucher_id_fkey");

            entity.HasOne(d => d.VoucherItemMember).WithMany(p => p.VoucherItems).HasConstraintName("voucher_items_voucher_item_member_id_fkey");
        });

        modelBuilder.Entity<VoucherItemMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("voucher_item_members_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.TotalPointsUsed).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Member).WithMany(p => p.VoucherItemMembers).HasConstraintName("voucher_item_members_member_id_fkey");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wallets_pkey");

            entity.Property(e => e.Balance).HasDefaultValueSql("0");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Points).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("wallets_user_id_fkey");
        });

        modelBuilder.Entity<WithdrawRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("withdraw_requests_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WithdrawRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("withdraw_requests_wallet_id_fkey");
        });


        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        // Messaging entities configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("conversations_pkey");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(now() AT TIME ZONE 'UTC')");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            
            entity.HasOne(d => d.Creator)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_conversations_created_by");
        });

        modelBuilder.Entity<ConversationMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("conversation_members_pkey");
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(now() AT TIME ZONE 'UTC')");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            
            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Members)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("fk_conversation_members_conversation");
                
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_conversation_members_user");
                
            entity.HasOne(d => d.LastReadMessage)
                .WithMany()
                .HasForeignKey(d => d.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_conversation_members_last_read_message");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("messages_pkey");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(now() AT TIME ZONE 'UTC')");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            
            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("fk_messages_conversation");
                
            entity.HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("fk_messages_sender");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
