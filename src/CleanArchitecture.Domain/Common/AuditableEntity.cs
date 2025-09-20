namespace CleanArchitecture.Domain.Common;

public abstract class AuditableEntity<T> : BaseEntity<T>
{
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    
    protected AuditableEntity() : base()
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    protected AuditableEntity(T id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }
    
    public void SetUpdatedBy(string updatedBy)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}