namespace capstone_backend.Business.Entities.Base;

/// <summary>
/// Base entity for all domain entities with common properties
/// </summary>
/// <remarks>
/// Provides audit fields (Created/Updated timestamps and user tracking),
/// soft delete functionality, and primary key
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key for the entity
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date and time when the entity was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created this entity
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who last updated this entity
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag - true if entity is deleted
    /// </summary>
    /// <remarks>
    /// Entities are never physically deleted from database for audit trail purposes.
    /// Use this flag to mark entities as deleted.
    /// </remarks>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date and time when the entity was soft deleted (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID who deleted this entity
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
