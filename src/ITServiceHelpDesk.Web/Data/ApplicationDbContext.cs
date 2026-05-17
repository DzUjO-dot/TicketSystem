using ITServiceHelpDesk.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Data;

/// <summary>
/// Główny kontekst bazy danych aplikacji
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ============================================
    // DB SETS
    // ============================================
    
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ============================================
    // MODEL CONFIGURATION
    // ============================================
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from current assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ============================================
        // TICKET CONFIGURATION
        // ============================================
        builder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            
            entity.HasIndex(t => t.TicketNumber)
                .IsUnique();
            
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.IsDeleted);

            entity.Property(t => t.TicketNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(t => t.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(t => t.ResolutionSummary)
                .HasMaxLength(2000);

            // Relationship: Ticket -> Category (many-to-one)
            entity.HasOne(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Ticket -> CreatedBy (many-to-one)
            entity.HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Ticket -> AssignedTo (many-to-one, nullable)
            entity.HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Query filter for soft delete
            entity.HasQueryFilter(t => !t.IsDeleted);
        });

        // ============================================
        // TICKET COMMENT CONFIGURATION
        // ============================================
        builder.Entity<TicketComment>(entity =>
        {
            entity.HasKey(c => c.Id);
            
            entity.HasIndex(c => c.TicketId);
            entity.HasIndex(c => c.CreatedAt);

            entity.Property(c => c.Content)
                .HasMaxLength(2000)
                .IsRequired();

            // Relationship: Comment -> Ticket
            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Comment -> Author
            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Query filter for soft delete
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        // ============================================
        // TICKET ATTACHMENT CONFIGURATION
        // ============================================
        builder.Entity<TicketAttachment>(entity =>
        {
            entity.HasKey(a => a.Id);
            
            entity.HasIndex(a => a.TicketId);

            entity.Property(a => a.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(a => a.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(a => a.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(a => a.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            // Relationship: Attachment -> Ticket
            entity.HasOne(a => a.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Attachment -> UploadedBy
            entity.HasOne(a => a.UploadedBy)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================
        // TICKET HISTORY CONFIGURATION
        // ============================================
        builder.Entity<TicketHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            
            entity.HasIndex(h => h.TicketId);
            entity.HasIndex(h => h.CreatedAt);

            entity.Property(h => h.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(h => h.OldValue)
                .HasMaxLength(500);

            entity.Property(h => h.NewValue)
                .HasMaxLength(500);

            entity.Property(h => h.Description)
                .HasMaxLength(500)
                .IsRequired();

            // Relationship: History -> Ticket
            entity.HasOne(h => h.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: History -> User
            entity.HasOne(h => h.User)
                .WithMany(u => u.TicketHistories)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================
        // CATEGORY CONFIGURATION
        // ============================================
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            
            entity.HasIndex(c => c.Name)
                .IsUnique();
            
            entity.HasIndex(c => c.IsActive);
            entity.HasIndex(c => c.DisplayOrder);

            entity.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.Icon)
                .HasMaxLength(50);

            entity.Property(c => c.Color)
                .HasMaxLength(7);
        });

        // ============================================
        // NOTIFICATION CONFIGURATION
        // ============================================
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.CreatedAt);

            entity.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(n => n.Message)
                .HasMaxLength(500)
                .IsRequired();

            // Relationship: Notification -> User
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Notification -> RelatedTicket (optional)
            entity.HasOne(n => n.RelatedTicket)
                .WithMany()
                .HasForeignKey(n => n.RelatedTicketId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // ============================================
        // AUDIT LOG CONFIGURATION
        // ============================================
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.EntityType);
            entity.HasIndex(a => a.CreatedAt);

            entity.Property(a => a.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.EntityId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(a => a.IpAddress)
                .HasMaxLength(45);

            entity.Property(a => a.UserAgent)
                .HasMaxLength(500);

            // Relationship: AuditLog -> User (optional)
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // ============================================
        // APPLICATION USER CONFIGURATION
        // ============================================
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Department)
                .HasMaxLength(100);

            entity.Property(u => u.PhoneExtension)
                .HasMaxLength(20);

            entity.Property(u => u.AvatarUrl)
                .HasMaxLength(255);

            entity.HasIndex(u => u.IsActive);
            entity.HasIndex(u => u.Email);
        });
    }

    // ============================================
    // SAVE CHANGES OVERRIDE - Auto UpdatedAt
    // ============================================
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Ticket ticket)
            {
                ticket.UpdatedAt = DateTime.Now;
            }
            else if (entry.Entity is TicketComment comment)
            {
                comment.UpdatedAt = DateTime.Now;
            }
        }
    }
}
