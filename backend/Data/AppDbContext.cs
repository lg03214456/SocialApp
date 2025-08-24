using SocialApp.Models;
using Microsoft.EntityFrameworkCore;

namespace SocialApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Friend> Friends => Set<Friend>();
}
