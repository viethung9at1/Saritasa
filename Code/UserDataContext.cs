using Microsoft.EntityFrameworkCore;
namespace Saritasa;
public class UserDataContext : DbContext
{
    public DbSet<RegularUser> RegularUsers { get; set; }
    public DbSet<File> Files { get; set; }
    public DbSet<Text> Texts { get; set; }
    public UserDataContext(DbContextOptions<UserDataContext> options) : base(options)
    {

    }
}