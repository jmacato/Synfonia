using Microsoft.EntityFrameworkCore;

namespace Synfonia.Backend
{
    public class LibraryDbContext : DbContext
    {
        private static volatile bool _dbInitialized;

        public LibraryDbContext()
        {
            if (_dbInitialized) return;

#if DEBUG
            // Database.EnsureDeleted();
#endif
            Database.EnsureCreated();

            _dbInitialized = true;
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Album>()
                .HasMany(c => c.Tracks)
                .WithOne(e => e.Album); 

            modelBuilder.Entity<Artist>()
                .HasMany(c => c.Albums)
                .WithOne(e => e.Artist); 
        }


        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Track> Tracks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=library-z.db");
    }
}