using Microsoft.EntityFrameworkCore;
namespace Saritasa;
public class UserDataContext : DbContext
{
    public DbSet<RegularUser> RegularUsers { get; set; }
    public DbSet<Upload> Uploads { get; set; }
    public UserDataContext(DbContextOptions<UserDataContext> options) : base(options)
    {

    }
}