using Microsoft.EntityFrameworkCore;

namespace PhotosDataBase.Model
{
    public class PhotosDbContext : DbContext
    {
        public PhotosDbContext(DbContextOptions<PhotosDbContext> options) : base(options) { }

        public DbSet<PhotoFileInfo> PhotoFiles { get; set; }
    }
}
