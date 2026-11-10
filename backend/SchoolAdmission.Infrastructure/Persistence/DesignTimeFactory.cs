using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolAdmission.Infrastructure.Persistence;

// Used only by 'dotnet ef' at design time. No DB connection is needed to create a migration.
public class DesignTimeFactory : IDesignTimeDbContextFactory<AdmissionDbContext>
{
    public AdmissionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AdmissionDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AdmissionDbContext(options);
    }
}
