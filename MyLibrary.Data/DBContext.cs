using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyLibrary.Data
{
    public class DBContext : DbContext
    {
        // Define DbSet properties for your models
        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<RentedRecord> RentedRecords { get; set; }
        public DbSet<SellRecord> SellRecords { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        // Constructor that passes options to the base DbContext
        public DBContext(DbContextOptions<DBContext> options)
            : base(options)
        {
        }

        // Optional: Add additional configuration if needed
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Username); // Set Username as the primary key
                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(450);
                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(u => u.Password)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(u => u.Gender)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Book entity configuration
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(b => b.Id); // Set Id as the primary key
                entity.Property(b => b.Id)
                    .IsRequired()
                    .HasMaxLength(450);
                entity.Property(b => b.Cover)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Author)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Title)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Publisher)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Borrowprice)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Buyprice)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(b => b.Year)
                    .IsRequired()
                    .HasMaxLength(4); // Assuming year is 4 digits
                entity.Property(b => b.SalePercentage)
                    .HasMaxLength(100); // Optional, set a reasonable length
            });
            
            modelBuilder.Entity<User>()
                .Property(u => u.Password)
                .HasMaxLength(256);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlServer("Server=SAJED;Database=MyLibraryDB;Trusted_Connection=True;TrustServerCertificate=True;")
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        }

    }
}
