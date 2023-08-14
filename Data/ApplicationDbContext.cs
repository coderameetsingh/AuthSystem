
using AuthSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationSystem
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        public DbSet<AuthModel>? Auths { get; set; }
    }
}
