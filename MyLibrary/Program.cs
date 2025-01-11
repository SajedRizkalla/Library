using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLibrary.Controllers;
using MyLibrary.Data;
using MyLibrary.Service.Services;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register your EF Core DBContext with SQL Server
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                // Number of retries
                maxRetryDelay: TimeSpan.FromSeconds(30), // Delay between retries
                errorNumbersToAdd: null                  // Additional error codes to retry on (optional)
            );
        }
    )
);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Users/LogIn";
        options.LogoutPath = "/Users/LogOut";
        options.AccessDeniedPath = "/Users/LogIn";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);    // Set cookie expiration
        options.SlidingExpiration = true;                  // Refresh the cookie on each request
    });

// 3. Register the iDataHelper services for your entities
builder.Services.AddScoped<iDataHelper<User>, UserEntity>();
builder.Services.AddScoped<iDataHelper<Book>, BookEntity>();
builder.Services.AddScoped<iDataHelper<RentedRecord>, RentedRecordEntity>();
builder.Services.AddScoped<iDataHelper<SellRecord>, SellRecordEntity>();
builder.Services.AddScoped<iDataHelper<CartItem>, CartItemEntity>();
builder.Services.AddScoped<iDataHelper<WaitingList>, WaitingListEntity>();
builder.Services.AddScoped<iDataHelper<PasswordResetRequest>, PasswordResetRequestEntity>();

// Configure Stripe
StripeConfiguration.ApiKey =
    "sk_test_51QeNRWKCjW1mTVvyvaWgwI3yBQHeoPSrE39nRJ9EAPUSThAJJKkwR5zirYBbniGBV3yoZrW3Fbsst9jgBjjZgQdF00a1Qw0486";

// Email settings (if applicable)
var emailSettings = builder.Configuration.GetSection("EmailSettings");
var email = emailSettings["Email"];
var password = emailSettings["Password"];

// Register NotificationService with email and password
builder.Services.AddSingleton(new NotificationService(email, password));

// 5. Add MVC with views and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 6. Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader();
    });
});

var app = builder.Build();

// 7. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 8. Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use CORS if configured
app.UseCors("AllowAll");

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// 9. Map your endpoints (MVC controllers + Razor Pages)
app.MapRazorPages();

// 10. Default MVC route (Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Index}");

// 11. Initialize database and seed data
DatabaseInitializer.Initialize(app.Services);

// 12. Run the application
app.Run();

public static class DatabaseInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();
        var userHelper = scope.ServiceProvider.GetRequiredService<iDataHelper<User>>();
        var bookHelper = scope.ServiceProvider.GetRequiredService<iDataHelper<Book>>();

        // Ensure the database is created
        context.Database.EnsureCreated();

        // Seed admin user
        if (!userHelper.GetData().Any(u => u.Username == "admin"))
        {
            var hashedPassword = HashPassword("123");
            var adminUser = new User
            {
                Username = "admin",
                Password = hashedPassword,
                Email = "admin@admin.com",
                IsAdmin = true,
                IsActive = true,
                Gender = "Male"
            };
            userHelper.Add(adminUser);
        }

        // Seed books if fewer than 25 exist in the database
        if (bookHelper.GetData().Count() < 25)
        {
            var books = GenerateBooks();
            foreach (var book in books)
            {
                bookHelper.Add(book);
            }
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    // =======================
    // SEED BOOKS (25 total)
    // =======================
    private static List<Book> GenerateBooks()
    {
        var books = new List<Book>
        {
            // 1 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Publisher = "Scribner",
                Genre = "drama",
                Buyprice = 7.99f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1925,
                DownloadUrl = "https://ct02210097.schoolwires.net/site/handlers/filedownload.ashx?moduleinstanceid=26616&dataid=28467&FileName=The%20Great%20Gatsby.pdf",
                Cover = "https://d28hgpri8am2if.cloudfront.net/book_images/onix/cvr9781524879761/the-great-gatsby-9781524879761_hr.jpg",

                IsJustForSell = true,
                AgeLimit = 16,
                SalePercentage = 10,
                SaleEndDate = DateTime.Now.AddDays(7)
            },

            // 2 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Pride and Prejudice",
                Author = "Jane Austen",
                Publisher = "T. Egerton",
                Genre = "comedy",
                Buyprice = 6.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1813,
                DownloadUrl = "https://giove.isti.cnr.it/demo/eread/Libri/joy/Pride.pdf",
                Cover = "https://cloud.firebrandtech.com/api/v2/image/111/9780785839866/CoverArtHigh/XL",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 5,
                SaleEndDate = DateTime.Now.AddDays(5)
            },

            // 3 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "1984",
                Author = "George Orwell",
                Publisher = "Secker & Warburg",
                Genre = "drama",
                Buyprice = 8.99f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1949,
                DownloadUrl = "https://rauterberg.employee.id.tue.nl/lecturenotes/DDM110%20CAS/Orwell-1949%201984.pdf",
                Cover = "https://images.booksense.com/images/913/287/9786257287913.jpg",

                IsJustForSell = true,
                AgeLimit = 18,
                SalePercentage = 10,
                SaleEndDate = DateTime.Now.AddDays(10)
            },

            // 4 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Moby-Dick",
                Author = "Herman Melville",
                Publisher = "Harper & Brothers",
                Genre = "comedy",
                Buyprice = 9.99f,
                Borrowprice = 2.99f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1851,
                DownloadUrl = "https://uberty.org/wp-content/uploads/2015/12/herman-melville-moby-dick.pdf",
                Cover = "https://m.media-amazon.com/images/I/71K4OH9CqOL._UF1000,1000_QL80_.jpg",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

           //// // 5 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "War and Peace",
                Author = "Leo Tolstoy",
                Publisher = "The Russian Messenger",
                Genre = "drama",
                Buyprice = 10.99f,
                Borrowprice = 3.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1869,
                DownloadUrl = "https://www.defence.lk/upload/ebooks/War%20And%20Peace.pdf",
                Cover = "https://cdn.kobo.com/book-images/e5191d85-2a23-480c-ae5e-e51947d26050/1200/1200/False/war-and-peace-aylmer-louise-maude-s-translation.jpg",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 15,
                SaleEndDate = DateTime.Now.AddDays(10)
            },

            // 6 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Jane Eyre",
                Author = "Charlotte Brontë",
                Publisher = "Smith, Elder & Co.",
                Genre = "drama",
                Buyprice = 7.49f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1847,
                DownloadUrl = "https://dbooks.bodleian.ox.ac.uk/books/PDFs/400269098.pdf",
                Cover = "https://trendsonline.pk/cdn/shop/files/94fdac62-cee7-4af0-bd61-291c360253e5.jpg?v=1718148970&width=1920",

                IsJustForSell = true,
                AgeLimit = 12,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 7 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Wuthering Heights",
                Author = "Emily Brontë",
                Publisher = "Thomas Cautley Newby",
                Genre = "drama",
                Buyprice = 6.99f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1847,
                DownloadUrl = "https://www.ucm.es/data/cont/docs/119-2014-04-09-Wuthering%20Heights.pdf",
                Cover = "https://m.media-amazon.com/images/I/81unikMK30L._AC_UF1000,1000_QL80_.jpg",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 5,
                SaleEndDate = DateTime.Now.AddDays(3)
            },

            // 8 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Frankenstein",
                Author = "Mary Shelley",
                Publisher = "Lackington, Hughes, Harding, Mavor & Jones",
                Genre = "horror",
                Buyprice = 5.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1818,
                DownloadUrl = "https://rauterberg.employee.id.tue.nl/lecturenotes/DDM110%20CAS/Shelley-1818%20Frankenstein.pdf",
                Cover = "https://m.media-amazon.com/images/I/917na8ZuvxL._AC_UF1000,1000_QL80_.jpg",

                IsJustForSell = true,
                AgeLimit = 16,
                SalePercentage = 20,
                SaleEndDate = DateTime.Now.AddDays(7)
            },

            // 9 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "A Tale of Two Cities",
                Author = "Charles Dickens",
                Publisher = "Chapman & Hall",
                Genre = "drama",
                Buyprice = 6.99f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1859,
                DownloadUrl = "https://www.gutenberg.org/files/98/old/2city12p.pdf",
                Cover = "https://m.media-amazon.com/images/I/813Yv-lescL._UF1000,1000_QL80_.jpg",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 10 - IsJustForSell = true
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "The Adventures of Sherlock Holmes",
                Author = "Arthur Conan Doyle",
                Publisher = "George Newnes",
                Genre = "comedy",
                Buyprice = 7.99f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1892,
                DownloadUrl = "https://sherlock-holm.es/stories/pdf/letter/1-sided/advs.pdf",
                Cover = "https://m.media-amazon.com/images/I/513VTCDLp9L._AC_UF1000,1000_QL80_.jpg",

                IsJustForSell = true,
                AgeLimit = 0,
                SalePercentage = 15,
                SaleEndDate = DateTime.Now.AddDays(5)
            },

            //// 11 - NOT just for sell (default false)
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Treasure Island",
                Author = "Robert Louis Stevenson",
                Publisher = "Cassell & Co.",
                Genre = "comedy",
                Buyprice = 5.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1883,
                DownloadUrl = "https://www.planetebook.com/free-ebooks/treasure-island.pdf",
                Cover = "https://m.media-amazon.com/images/I/71fi+jN6QpS._UF1000,1000_QL80_.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 12
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Pride and Prejudice",
                Author = "Jane Austen",
                Publisher = "T. Egerton",
                Genre = "comedy",
                Buyprice = 6.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1813,
                DownloadUrl = "https://giove.isti.cnr.it/demo/eread/Libri/joy/Pride.pdf",
                Cover = "https://m.media-amazon.com/images/M/MV5BYzNkMjRmZGMtODg1Ni00MjIxLWI4MTYtOGEwM2YyMmZiMjUzXkEyXkFqcGc@._V1_.jpg",

                AgeLimit = 0,
                SalePercentage = 5,
                SaleEndDate = DateTime.Now.AddDays(5),
                IsJustForSell = false
            },

            // 13
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Alice's Adventures in Wonderland",
                Author = "Lewis Carroll",
                Publisher = "Macmillan",
                Genre = "comedy",
                Buyprice = 6.49f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1865,
                DownloadUrl = "https://www.adobe.com/be_en/active-use/pdf/Alice_in_Wonderland.pdf",
                Cover = "https://m.media-amazon.com/images/I/71PdNDqqDzL._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 14
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Dracula",
                Author = "Bram Stoker",
                Publisher = "Archibald Constable and Company",
                Genre = "horror",
                Buyprice = 6.99f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1897,
                DownloadUrl = "https://www.bramstoker.org/pdf/novels/05dracula.pdf",
                Cover = "https://m.media-amazon.com/images/I/91wOUFZCE+L._UF1000,1000_QL80_.jpg",

                AgeLimit = 18,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 15
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "The Picture of Dorian Gray",
                Author = "Oscar Wilde",
                Publisher = "Ward, Lock & Co.",
                Genre = "drama",
                Buyprice = 5.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 2,
                Year = 1890,
                DownloadUrl = "https://web2.mlp.cz/koweb/00/04/30/22/80/the_picture_of_dorian_gray.pdf",
                Cover = "https://m.media-amazon.com/images/I/71q3FNqEIQL._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 16,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 16
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Crime and Punishment",
                Author = "Fyodor Dostoevsky",
                Publisher = "The Russian Messenger",
                Genre = "drama",
                Buyprice = 8.99f,
                Borrowprice = 2.99f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1866,
                DownloadUrl = "http://giove.isti.cnr.it/demo/eread/Libri/angry/Crime.pdf",
                Cover = "https://images.penguinrandomhouse.com/cover/9780553211757",

                AgeLimit = 16,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 17
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "The Brothers Karamazov",
                Author = "Fyodor Dostoevsky",
                Publisher = "The Russian Messenger",
                Genre = "drama",
                Buyprice = 9.49f,
                Borrowprice = 2.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1880,
                DownloadUrl = "https://www.gutenberg.org/files/28054/old/28054-pdf.pdf",
                Cover = "https://m.media-amazon.com/images/I/71OZJsgZzQL.jpg",

                AgeLimit = 16,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 18
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Don Quixote",
                Author = "Miguel de Cervantes",
                Publisher = "Francisco de Robles",
                Genre = "comedy",
                Buyprice = 8.99f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1605,
                DownloadUrl = "https://www.pinkmonkey.com/dl/library1/book0530.pdf",
                Cover = "https://m.media-amazon.com/images/I/71humHwIT6L._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 12,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 19
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Gulliver's Travels",
                Author = "Jonathan Swift",
                Publisher = "Benjamin Motte",
                Genre = "comedy",
                Buyprice = 5.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1726,
                DownloadUrl = "https://www.eriesd.org/cms/lib/PA01001942/Centricity/Domain/2098/Gullivers%20Travels%20text.pdf",
                Cover = "https://m.media-amazon.com/images/I/81BzA3z+N9L._UF1000,1000_QL80_.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 20
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "The Count of Monte Cristo",
                Author = "Alexandre Dumas",
                Publisher = "Penguin Classics (modern edition)",
                Genre = "drama",
                Buyprice = 7.99f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1844,
                DownloadUrl = "https://www.epedagogia.com.br/materialbibliotecaonine/797The-Count-of-Monte-Cristo.pdf",
                Cover = "https://images-na.ssl-images-amazon.com/images/S/compressed.photo.goodreads.com/books/1692339248i/197029269.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 21
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Adventures of Huckleberry Finn",
                Author = "Mark Twain",
                Publisher = "Chatto & Windus / Charles L. Webster And Company",
                Genre = "comedy",
                Buyprice = 6.99f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1884,
                DownloadUrl = "https://contentserver.adobe.com/store/books/HuckFinn.pdf",
                Cover = "https://dover-books-us.imgix.net/covers/9780486836171.jpg?auto=format&w=300",

                AgeLimit = 12,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 22
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Sense and Sensibility",
                Author = "Jane Austen",
                Publisher = "Thomas Egerton",
                Genre = "drama",
                Buyprice = 5.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1811,
                DownloadUrl = "https://www.gutenberg.org/files/161/old/sense11p.pdf",
                Cover = "https://m.media-amazon.com/images/I/7161NWQ0jrL._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 23
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Ulysses",
                Author = "James Joyce",
                Publisher = "Sylvia Beach",
                Genre = "drama",
                Buyprice = 8.49f,
                Borrowprice = 2.49f,
                RentQuantity = 3,
                SellQuantity = 5,
                Year = 1922,
                DownloadUrl = "https://web.itu.edu.tr/inceogl4/modernism/Ulysses.pdf",
                Cover = "https://covers.storytel.com/jpg-640/9789175710372.75266914-66e4-46e2-927f-9bcf0282c701?optimize=high",

                AgeLimit = 18,
                SalePercentage = 0,
                SaleEndDate = null
            },

            //// 24
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Heart of Darkness",
                Author = "Joseph Conrad",
                Publisher = "Blackwood's Magazine",
                Genre = "horror",
                Buyprice = 4.99f,
                Borrowprice = 1.49f,
                RentQuantity = 3,
                SellQuantity = 3,
                Year = 1899,
                DownloadUrl = "https://www.planetebook.com/free-ebooks/heart-of-darkness.pdf",
                Cover = "https://m.media-amazon.com/images/I/71jDQ-T5e0L._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 16,
                SalePercentage = 0,
                SaleEndDate = null
            },

            // 25
            new Book
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Oliver Twist",
                Author = "Charles Dickens",
                Publisher = "Richard Bentley",
                Genre = "drama",
                Buyprice = 6.99f,
                Borrowprice = 1.99f,
                RentQuantity = 3,
                SellQuantity = 4,
                Year = 1838,
                DownloadUrl = "https://e-school.kmutt.ac.th/elibrary/Upload/EBook/DSIL_Lib_E1312881157.pdf",
                Cover = "https://m.media-amazon.com/images/I/81QGqaKWjXL._AC_UF1000,1000_QL80_.jpg",

                AgeLimit = 0,
                SalePercentage = 0,
                SaleEndDate = null
            }
        };

        return books;
    }
}
