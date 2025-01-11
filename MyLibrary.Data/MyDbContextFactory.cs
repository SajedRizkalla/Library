using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using MyLibrary.Data;

public class MyDbContextFactory : IDesignTimeDbContextFactory<DBContext>
{
    public DBContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();

        // Configure the connection string
        optionsBuilder.UseSqlServer("Server=SAJED;Database=MyLibraryDB;Trusted_Connection=True;TrustServerCertificate=True;");

        return new DBContext(optionsBuilder.Options);
    }
}