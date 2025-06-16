using Microsoft.EntityFrameworkCore;
using WebDriveBackend.Entities;

namespace WebDriveBackend;

public class Database(DbContextOptions options) : DbContext(options)
{
    public DbSet<UserIdentity> UsersIdentity { get; set; }
    public DbSet<UserProfile> UsersProfile { get; set; }
    public DbSet<UserStorage> UsersStorage { get; set; }
}