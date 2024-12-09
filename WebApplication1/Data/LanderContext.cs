using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lander.Data
{
    public class LanderContext : IdentityDbContext
    {
        public LanderContext(DbContextOptions<LanderContext> options) : base(options) { }
    }
}
