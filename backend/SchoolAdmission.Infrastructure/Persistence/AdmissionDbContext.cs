using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Infrastructure.Persistence;

public class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : base(options) { }

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Shift>(e =>
        {
            e.ToTable("shifts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes", t => t.HasCheckConstraint("ck_classes_total_seats", "total_seats > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Shift).WithMany(s => s.Classes).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Name, x.ShiftId }).IsUnique();
        });

        b.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).HasMaxLength(17).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Class).WithMany(c => c.Holds).HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ClassId, x.Status, x.ExpiresAt });
            e.HasIndex(x => new { x.StudentId, x.CreatedAt });

            // Database-level safety net: one active application per student per class.
            e.HasIndex(x => new { x.StudentId, x.ClassId })
             .IsUnique()
             .HasFilter("status IN ('Held','Confirmed')");
        });
    }
}
