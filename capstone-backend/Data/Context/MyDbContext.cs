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

    public virtual DbSet<ads_order> ads_orders { get; set; }

    public virtual DbSet<advertisement> advertisements { get; set; }

    public virtual DbSet<advertisement_package> advertisement_packages { get; set; }

    public virtual DbSet<blog> blogs { get; set; }

    public virtual DbSet<blog_like> blog_likes { get; set; }

    public virtual DbSet<challenge> challenges { get; set; }

    public virtual DbSet<check_in_history> check_in_histories { get; set; }

    public virtual DbSet<collection> collections { get; set; }

    public virtual DbSet<comment> comments { get; set; }

    public virtual DbSet<comment_like> comment_likes { get; set; }

    public virtual DbSet<couple_mood_log> couple_mood_logs { get; set; }

    public virtual DbSet<couple_mood_type> couple_mood_types { get; set; }

    public virtual DbSet<couple_personality_type> couple_personality_types { get; set; }

    public virtual DbSet<couple_profile> couple_profiles { get; set; }

    public virtual DbSet<couple_profile_challenge> couple_profile_challenges { get; set; }

    public virtual DbSet<date_plan> date_plans { get; set; }

    public virtual DbSet<date_plan_item> date_plan_items { get; set; }

    public virtual DbSet<device_token> device_tokens { get; set; }

    public virtual DbSet<leaderboard> leaderboards { get; set; }

    public virtual DbSet<location_follower> location_followers { get; set; }

    public virtual DbSet<location_tag> location_tags { get; set; }

    public virtual DbSet<medium> media { get; set; }

    public virtual DbSet<member_accessory> member_accessories { get; set; }

    public virtual DbSet<member_mood_log> member_mood_logs { get; set; }

    public virtual DbSet<member_profile> member_profiles { get; set; }

    public virtual DbSet<member_subscription_package> member_subscription_packages { get; set; }

    public virtual DbSet<mood_type> mood_types { get; set; }

    public virtual DbSet<notification> notifications { get; set; }

    public virtual DbSet<owner_member> owner_members { get; set; }

    public virtual DbSet<personality_test> personality_tests { get; set; }

    public virtual DbSet<question> questions { get; set; }

    public virtual DbSet<question_answer> question_answers { get; set; }

    public virtual DbSet<refresh_token> refresh_tokens { get; set; }

    public virtual DbSet<report> reports { get; set; }

    public virtual DbSet<report_type> report_types { get; set; }

    public virtual DbSet<review> reviews { get; set; }

    public virtual DbSet<review_like> review_likes { get; set; }

    public virtual DbSet<search_history> search_histories { get; set; }

    public virtual DbSet<special_event> special_events { get; set; }

    public virtual DbSet<subscription_package> subscription_packages { get; set; }

    public virtual DbSet<test_type> test_types { get; set; }

    public virtual DbSet<top_search> top_searches { get; set; }

    public virtual DbSet<transaction> transactions { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    public virtual DbSet<venue_location> venue_locations { get; set; }

    public virtual DbSet<venue_location_advertisement> venue_location_advertisements { get; set; }

    public virtual DbSet<venue_owner_profile> venue_owner_profiles { get; set; }

    public virtual DbSet<venue_subscription_package> venue_subscription_packages { get; set; }

    public virtual DbSet<voucher> vouchers { get; set; }

    public virtual DbSet<voucher_item> voucher_items { get; set; }

    public virtual DbSet<voucher_item_member> voucher_item_members { get; set; }

    public virtual DbSet<wallet> wallets { get; set; }

    public virtual DbSet<withdraw_request> withdraw_requests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<ads_order>(entity =>
        {
            entity.HasKey(e => e.id).HasName("ads_orders_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.advertisement).WithMany(p => p.ads_orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ads_orders_advertisement_id_fkey");

            entity.HasOne(d => d.package).WithMany(p => p.ads_orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ads_orders_package_id_fkey");
        });

        modelBuilder.Entity<advertisement>(entity =>
        {
            entity.HasKey(e => e.id).HasName("advertisements_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.venue_owner).WithMany(p => p.advertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("advertisements_venue_owner_id_fkey");
        });

        modelBuilder.Entity<advertisement_package>(entity =>
        {
            entity.HasKey(e => e.id).HasName("advertisement_packages_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.priority_score).HasDefaultValue(1);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<blog>(entity =>
        {
            entity.HasKey(e => e.id).HasName("blogs_pkey");

            entity.Property(e => e.comment_count).HasDefaultValue(0);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.like_count).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.blogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("blogs_member_id_fkey");
        });

        modelBuilder.Entity<blog_like>(entity =>
        {
            entity.HasKey(e => e.id).HasName("blog_likes_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.blog).WithMany(p => p.blog_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("blog_likes_blog_id_fkey");

            entity.HasOne(d => d.member).WithMany(p => p.blog_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("blog_likes_member_id_fkey");
        });

        modelBuilder.Entity<challenge>(entity =>
        {
            entity.HasKey(e => e.id).HasName("challenges_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.reward_points).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<check_in_history>(entity =>
        {
            entity.HasKey(e => e.id).HasName("check_in_histories_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_valid).HasDefaultValue(true);

            entity.HasOne(d => d.member).WithMany(p => p.check_in_histories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_histories_member_id_fkey");

            entity.HasOne(d => d.venue).WithMany(p => p.check_in_histories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("check_in_histories_venue_id_fkey");
        });

        modelBuilder.Entity<collection>(entity =>
        {
            entity.HasKey(e => e.id).HasName("collections_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("collections_member_id_fkey");

            entity.HasMany(d => d.venues).WithMany(p => p.collections)
                .UsingEntity<Dictionary<string, object>>(
                    "collection_venue_location",
                    r => r.HasOne<venue_location>().WithMany()
                        .HasForeignKey("venue_id")
                        .HasConstraintName("collection_venue_locations_venue_id_fkey"),
                    l => l.HasOne<collection>().WithMany()
                        .HasForeignKey("collection_id")
                        .HasConstraintName("collection_venue_locations_collection_id_fkey"),
                    j =>
                    {
                        j.HasKey("collection_id", "venue_id").HasName("collection_venue_locations_pkey");
                        j.ToTable("collection_venue_locations");
                    });
        });

        modelBuilder.Entity<comment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("comments_pkey");

            entity.Property(e => e.comment_count).HasDefaultValue(0);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.like_count).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.blog).WithMany(p => p.comments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comments_blog_id_fkey");

            entity.HasOne(d => d.member).WithMany(p => p.comments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comments_member_id_fkey");
        });

        modelBuilder.Entity<comment_like>(entity =>
        {
            entity.HasKey(e => e.id).HasName("comment_likes_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.comment).WithMany(p => p.comment_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comment_likes_comment_id_fkey");

            entity.HasOne(d => d.member).WithMany(p => p.comment_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("comment_likes_member_id_fkey");
        });

        modelBuilder.Entity<couple_mood_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_mood_logs_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.couple).WithMany(p => p.couple_mood_logs).HasConstraintName("couple_mood_logs_couple_id_fkey");

            entity.HasOne(d => d.couple_mood_type).WithMany(p => p.couple_mood_logs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_mood_logs_couple_mood_type_id_fkey");
        });

        modelBuilder.Entity<couple_mood_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_mood_types_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<couple_personality_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_personality_types_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<couple_profile>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_profiles_pkey");

            entity.Property(e => e.budget_min).HasDefaultValueSql("0");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.interaction_points).HasDefaultValue(0);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.total_points).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.couple_mood_type).WithMany(p => p.couple_profiles).HasConstraintName("couple_profiles_couple_mood_type_id_fkey");

            entity.HasOne(d => d.couple_personality_type).WithMany(p => p.couple_profiles).HasConstraintName("couple_profiles_couple_personality_type_id_fkey");

            entity.HasOne(d => d.member_id_1Navigation).WithMany(p => p.couple_profilemember_id_1Navigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_profiles_member_id_1_fkey");

            entity.HasOne(d => d.member_id_2Navigation).WithMany(p => p.couple_profilemember_id_2Navigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("couple_profiles_member_id_2_fkey");
        });

        modelBuilder.Entity<couple_profile_challenge>(entity =>
        {
            entity.HasKey(e => e.id).HasName("couple_profile_challenges_pkey");

            entity.Property(e => e.completed_member_ids).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.current_progress).HasDefaultValue(0);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.challenge).WithMany(p => p.couple_profile_challenges).HasConstraintName("couple_profile_challenges_challenge_id_fkey");

            entity.HasOne(d => d.couple).WithMany(p => p.couple_profile_challenges).HasConstraintName("couple_profile_challenges_couple_id_fkey");
        });

        modelBuilder.Entity<date_plan>(entity =>
        {
            entity.HasKey(e => e.id).HasName("date_plans_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.estimated_budget).HasDefaultValueSql("0");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.couple).WithMany(p => p.date_plans).HasConstraintName("date_plans_couple_id_fkey");
        });

        modelBuilder.Entity<date_plan_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("date_plan_items_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.order_index).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.date_plan).WithMany(p => p.date_plan_items).HasConstraintName("date_plan_items_date_plan_id_fkey");

            entity.HasOne(d => d.venue_location).WithMany(p => p.date_plan_items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("date_plan_items_venue_location_id_fkey");
        });

        modelBuilder.Entity<device_token>(entity =>
        {
            entity.HasKey(e => e.id).HasName("device_tokens_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithMany(p => p.device_tokens).HasConstraintName("device_tokens_user_id_fkey");
        });

        modelBuilder.Entity<leaderboard>(entity =>
        {
            entity.HasKey(e => e.id).HasName("leaderboards_pkey");

            entity.Property(e => e.total_points).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.couple).WithMany(p => p.leaderboards).HasConstraintName("leaderboards_couple_id_fkey");
        });

        modelBuilder.Entity<location_follower>(entity =>
        {
            entity.HasKey(e => e.id).HasName("location_followers_pkey");

            entity.ToTable(tb => tb.HasComment("Bảng quan hệ theo dõi / chia sẻ vị trí giữa users"));

            entity.Property(e => e.id).UseIdentityAlwaysColumn();
            entity.Property(e => e.created_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.follower_share_status).HasDefaultValueSql("'SHARING'::character varying");
            entity.Property(e => e.follower_user_id).HasComment("User theo dõi");
            entity.Property(e => e.is_muted).HasDefaultValue(false);
            entity.Property(e => e.owner_share_status).HasDefaultValueSql("'SHARING'::character varying");
            entity.Property(e => e.owner_user_id).HasComment("User trung tâm");
            entity.Property(e => e.status)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasComment("ACTIVE, REMOVED, BLOCKED, PENDING");
            entity.Property(e => e.updated_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<location_tag>(entity =>
        {
            entity.HasKey(e => e.id).HasName("location_tags_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.couple_mood_type).WithMany(p => p.location_tags).HasConstraintName("location_tags_couple_mood_type_id_fkey");

            entity.HasOne(d => d.couple_personality_type).WithMany(p => p.location_tags).HasConstraintName("location_tags_couple_personality_type_id_fkey");
        });

        modelBuilder.Entity<medium>(entity =>
        {
            entity.HasKey(e => e.id).HasName("media_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<member_accessory>(entity =>
        {
            entity.HasKey(e => e.id).HasName("member_accessories_pkey");

            entity.Property(e => e.acquired_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_equipped).HasDefaultValue(false);

            entity.HasOne(d => d.accessory).WithMany(p => p.member_accessories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("member_accessories_accessory_id_fkey");

            entity.HasOne(d => d.member).WithMany(p => p.member_accessories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("member_accessories_member_id_fkey");
        });

        modelBuilder.Entity<member_mood_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("member_mood_logs_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.is_private).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.member_mood_logs).HasConstraintName("member_mood_logs_member_id_fkey");

            entity.HasOne(d => d.mood_type).WithMany(p => p.member_mood_logs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_mood_logs_mood_type_id_fkey");
        });

        modelBuilder.Entity<member_profile>(entity =>
        {
            entity.HasKey(e => e.id).HasName("member_profiles_pkey");

            entity.Property(e => e.budget_min).HasDefaultValueSql("0");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.relationship_status).HasDefaultValueSql("'SINGLE'::text");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.mood_types).WithMany(p => p.member_profiles).HasConstraintName("member_profiles_mood_types_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.member_profiles).HasConstraintName("member_profiles_user_id_fkey");
        });

        modelBuilder.Entity<member_subscription_package>(entity =>
        {
            entity.HasKey(e => e.id).HasName("member_subscription_packages_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.member_subscription_packages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_subscription_packages_member_id_fkey");

            entity.HasOne(d => d.package).WithMany(p => p.member_subscription_packages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("member_subscription_packages_package_id_fkey");
        });

        modelBuilder.Entity<mood_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("mood_types_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<notification>(entity =>
        {
            entity.HasKey(e => e.id).HasName("notifications_pkey");

            entity.HasIndex(e => e.user_id, "idx_noti_user_unread").HasFilter("(is_read = false)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_read).HasDefaultValue(false);

            entity.HasOne(d => d.user).WithMany(p => p.notifications).HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<owner_member>(entity =>
        {
            entity.HasKey(e => new { e.owner_user_id, e.member_user_id }).HasName("owner_members_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.updated_at).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<personality_test>(entity =>
        {
            entity.HasKey(e => e.id).HasName("personality_tests_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.personality_tests).HasConstraintName("personality_tests_member_id_fkey");

            entity.HasOne(d => d.test_type).WithMany(p => p.personality_tests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("personality_tests_test_type_id_fkey");
        });

        modelBuilder.Entity<question>(entity =>
        {
            entity.HasKey(e => e.id).HasName("questions_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.order_index).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.test_type).WithMany(p => p.questions).HasConstraintName("questions_test_type_id_fkey");
        });

        modelBuilder.Entity<question_answer>(entity =>
        {
            entity.HasKey(e => e.id).HasName("question_answers_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.order_index).HasDefaultValue(0);
            entity.Property(e => e.score_value).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.question).WithMany(p => p.question_answers).HasConstraintName("question_answers_question_id_fkey");
        });

        modelBuilder.Entity<refresh_token>(entity =>
        {
            entity.HasKey(e => e.id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithMany(p => p.refresh_tokens).HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<report>(entity =>
        {
            entity.HasKey(e => e.id).HasName("reports_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.reporter).WithMany(p => p.reports).HasConstraintName("reports_reporter_id_fkey");
        });

        modelBuilder.Entity<report_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("report_types_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<review>(entity =>
        {
            entity.HasKey(e => e.id).HasName("reviews_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_anonymous).HasDefaultValue(false);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.like_count).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reviews_member_id_fkey");

            entity.HasOne(d => d.venue).WithMany(p => p.reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reviews_venue_id_fkey");
        });

        modelBuilder.Entity<review_like>(entity =>
        {
            entity.HasKey(e => e.id).HasName("review_likes_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.review_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("review_likes_member_id_fkey");

            entity.HasOne(d => d.review).WithMany(p => p.review_likes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("review_likes_review_id_fkey");
        });

        modelBuilder.Entity<search_history>(entity =>
        {
            entity.HasKey(e => e.id).HasName("search_histories_pkey");

            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.result_count).HasDefaultValue(0);
            entity.Property(e => e.searched_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.search_histories)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("search_histories_member_id_fkey");
        });

        modelBuilder.Entity<special_event>(entity =>
        {
            entity.HasKey(e => e.id).HasName("special_events_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<subscription_package>(entity =>
        {
            entity.HasKey(e => e.id).HasName("subscription_packages_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.price).HasDefaultValueSql("0");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<test_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("test_types_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.total_questions).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<top_search>(entity =>
        {
            entity.HasKey(e => e.id).HasName("top_searches_pkey");

            entity.Property(e => e.hit_count).HasDefaultValue(0);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.last_updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<transaction>(entity =>
        {
            entity.HasKey(e => e.id).HasName("transactions_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
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

        modelBuilder.Entity<venue_location>(entity =>
        {
            entity.HasKey(e => e.id).HasName("venue_locations_pkey");

            entity.Property(e => e.avarage_cost).HasDefaultValueSql("0");
            entity.Property(e => e.average_rating).HasDefaultValueSql("0");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.is_open).HasDefaultValue(true);
            entity.Property(e => e.price_min).HasDefaultValueSql("0");
            entity.Property(e => e.review_count).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.location_tag).WithMany(p => p.venue_locations).HasConstraintName("venue_locations_location_tag_id_fkey");

            entity.HasOne(d => d.venue_owner).WithMany(p => p.venue_locations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_locations_venue_owner_id_fkey");
        });

        modelBuilder.Entity<venue_location_advertisement>(entity =>
        {
            entity.HasKey(e => e.id).HasName("venue_location_advertisements_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.priority_score).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.advertisement).WithMany(p => p.venue_location_advertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_location_advertisements_advertisement_id_fkey");

            entity.HasOne(d => d.venue).WithMany(p => p.venue_location_advertisements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_location_advertisements_venue_id_fkey");
        });

        modelBuilder.Entity<venue_owner_profile>(entity =>
        {
            entity.HasKey(e => e.id).HasName("venue_owner_profiles_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithMany(p => p.venue_owner_profiles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_owner_profiles_user_id_fkey");
        });

        modelBuilder.Entity<venue_subscription_package>(entity =>
        {
            entity.HasKey(e => e.id).HasName("venue_subscription_packages_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.package).WithMany(p => p.venue_subscription_packages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venue_subscription_packages_package_id_fkey");

            entity.HasOne(d => d.venue).WithMany(p => p.venue_subscription_packages).HasConstraintName("venue_subscription_packages_venue_id_fkey");
        });

        modelBuilder.Entity<voucher>(entity =>
        {
            entity.HasKey(e => e.id).HasName("vouchers_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.usage_limit_per_member).HasDefaultValue(1);

            entity.HasOne(d => d.venue_owner).WithMany(p => p.vouchers).HasConstraintName("vouchers_venue_owner_id_fkey");
        });

        modelBuilder.Entity<voucher_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("voucher_items_pkey");

            entity.Property(e => e.acquired_at).HasDefaultValueSql("now()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.voucher).WithMany(p => p.voucher_items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("voucher_items_voucher_id_fkey");

            entity.HasOne(d => d.voucher_item_member).WithMany(p => p.voucher_items).HasConstraintName("voucher_items_voucher_item_member_id_fkey");
        });

        modelBuilder.Entity<voucher_item_member>(entity =>
        {
            entity.HasKey(e => e.id).HasName("voucher_item_members_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.quantity).HasDefaultValue(1);
            entity.Property(e => e.total_points_used).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.member).WithMany(p => p.voucher_item_members).HasConstraintName("voucher_item_members_member_id_fkey");
        });

        modelBuilder.Entity<wallet>(entity =>
        {
            entity.HasKey(e => e.id).HasName("wallets_pkey");

            entity.Property(e => e.balance).HasDefaultValueSql("0");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.points).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithMany(p => p.wallets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("wallets_user_id_fkey");
        });

        modelBuilder.Entity<withdraw_request>(entity =>
        {
            entity.HasKey(e => e.id).HasName("withdraw_requests_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.requested_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.wallet).WithMany(p => p.withdraw_requests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("withdraw_requests_wallet_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
