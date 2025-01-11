using MyLibrary.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MyLibrary.Data.DTOS;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MyLibrary.Core;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using MyLibrary.Service.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace MyLibrary.Controllers
{
    public class UsersController : Controller
    {
        private readonly iDataHelper<User> dataHelper;
        private readonly iDataHelper<Book> bookDataHelper;
        private readonly iDataHelper<RentedRecord> rentedRecordHelper;
        private readonly iDataHelper<SellRecord> sellRecordHelper;
        private readonly iDataHelper<PasswordResetRequest> resetRequestHelper;
        private readonly NotificationService notificationService;
        private readonly DBContext _context;


        public UsersController(iDataHelper<User> dataHelper, NotificationService n, iDataHelper<Book> bookDataHelper,
            iDataHelper<SellRecord> sellRecordHelper, iDataHelper<RentedRecord> rentedRecordHelper,iDataHelper<PasswordResetRequest> resetRequestHelper, DBContext context)
        {
            this.dataHelper = dataHelper;
            this.bookDataHelper = bookDataHelper;
            this.rentedRecordHelper = rentedRecordHelper;
            this.sellRecordHelper = sellRecordHelper;
            this.resetRequestHelper = resetRequestHelper;
            this.notificationService = n;
            this._context = context;
        }

        // Main page showing books
        public IActionResult Index()
        {
            // Fetch books and their data
            var books = bookDataHelper.GetData()
                .OrderByDescending(b => b.Buyprice)
                .ToList();

            // Fetch ratings from the database
            var websiteRatings = _context.Ratings
                .Where(r => r.IsWebsiteRating) // Only website ratings
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            // Calculate overall website rating
            double overallRating = websiteRatings
                .Where(r => r.RatingValue > 0) // Exclude ratings with a value of 0
                .Any()
                ? websiteRatings.Where(r => r.RatingValue > 0).Average(r => r.RatingValue ?? 0)
                : 0;


            // Pass ratings and overall rating to ViewBag
            ViewBag.WebsiteRatings = websiteRatings;
            ViewBag.OverallRating = overallRating;

            return View(books);
        }



        public IActionResult MyBooks()
        {
            var username = User.Identity.Name;

            // 1) Gather user's RentedRecords and SellRecords
            var rentedBooks = _context.RentedRecords
                .Where(r => r.Username == username)
                .ToList();

            var purchasedBooks = _context.SellRecords
                .Where(p => p.Username == username)
                .ToList();

            // 2) Collect all Book IDs
            var bookIds = rentedBooks.Select(r => r.BookId)
                .Union(purchasedBooks.Select(p => p.BookId))
                .ToList();

            // 3) Gather Ratings (grouped by BookId)
            var ratings = _context.Ratings
                .Where(r => bookIds.Contains(r.BookId) && r.BookId != null)
                .GroupBy(r => r.BookId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Ensure all books have a key in the dictionary, even if no ratings exist
            foreach (var bookId in bookIds)
            {
                if (!ratings.ContainsKey(bookId))
                {
                    ratings[bookId] = new List<Rating>();
                }
            }

            // 4) Load the actual Book objects for these IDs
            var booksDict = _context.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToDictionary(b => b.Id, b => b);

            // 5) Build your MyBooksViewModel, adding BookDict
            var viewModel = new Data.DTOS.BookDTOs.MyBooksViewModel
            {
                RentedBooks = rentedBooks,
                PurchasedBooks = purchasedBooks,
                Ratings = ratings,
                BookDict = booksDict
            };

            return View(viewModel);
        }





        // Search and filter books
        public IActionResult Search(string query, int? minPrice, int? maxPrice, string bookType)
        {
            var books = bookDataHelper.GetData();

            if (!string.IsNullOrEmpty(query))
            {
                books = books.Where(b => b.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         b.Author.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (minPrice.HasValue)
            {
                books = books.Where(b => b.Buyprice >= minPrice).ToList();
            }

            if (maxPrice.HasValue)
            {
                books = books.Where(b => b.Buyprice <= maxPrice).ToList();
            }

            if (!string.IsNullOrEmpty(bookType))
            {
                books = books.Where(b => b.Genre.Equals(bookType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View("Index", books); // Reuse the "Index.cshtml" view to show filtered books
        }

        // User profile page
        [Authorize]
        public IActionResult ProfilePage()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("LogIn", "Users");
            }

            var user = dataHelper.GetData().FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("LogIn", "Users");
            }

            var editRequest = new UserDTOs.EditRequest
            {
                Username = user.Username,
                Email = user.Email,
                Password = user.Password,
                Gender = user.Gender,
                OldUsername = user.Username
            };

            return View(editRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(UserDTOs.EditRequest updatedUser)
        {
            try
            {
                // Find the existing user using OldUsername
                var existingUser = dataHelper.Find(updatedUser.OldUsername);
                if (existingUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View("ProfilePage", updatedUser);
                }

                // Check if the new Username already exists (and is not the old one)
                if (updatedUser.Username != updatedUser.OldUsername && dataHelper.Find(updatedUser.Username) != null)
                {
                    ModelState.AddModelError(string.Empty,
                        "The new username is already taken. Please choose a different one.");
                    return View("ProfilePage", updatedUser);
                }

                // If the username is changing, delete the old user and add the new one
                if (updatedUser.Username != updatedUser.OldUsername)
                {
                    // Delete the old user
                    dataHelper.Delete(updatedUser.OldUsername);

                    // Create a new user with the updated details
                    var newUser = new User
                    {
                        Username = updatedUser.Username,
                        Email = updatedUser.Email,
                        Password = updatedUser.Password == existingUser.Password
                            ? existingUser.Password
                            : HashPassword(updatedUser.Password),
                        Gender = existingUser
                            .Gender // Preserve the existing Gender (or take from updatedUser if applicable)
                    };

                    dataHelper.Add(newUser);
                }
                else
                {
                    // Update the existing user's properties
                    existingUser.Email = updatedUser.Email;

                    // Check if the password is different before updating it
                    if (!string.IsNullOrEmpty(updatedUser.Password) &&
                        !ValidatePassword(updatedUser.Password, existingUser.Password))
                    {
                        ModelState.AddModelError(string.Empty, "The current password is incorrect.");
                        return View("ProfilePage", updatedUser);
                    }

                    // Update password only if it is being changed
                    if (!string.IsNullOrEmpty(updatedUser.Password))
                    {
                        existingUser.Password = HashPassword(updatedUser.Password);
                    }

                    // Save the updated user
                    dataHelper.Edit(updatedUser.OldUsername, existingUser);
                }

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("ProfilePage");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating profile: {ex.Message}");
            }

            // Return to the profile page with validation errors
            return View("ProfilePage", updatedUser);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LogIn", "Users");
        }

        // User signup
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(User user)
        {
            try
            {
                // Hash the user's password before saving
                user.Password = HashPassword(user.Password);

                dataHelper.Add(user); // Save the user with the hashed password

                TempData["SuccessMessage"] = "Signup successful! You can now log in.";
                return RedirectToAction("LogIn", "Users"); // Redirect to Login page
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(user); // Stay on the same page with validation errors
            }
        }


        // Hash password using SHA-256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool ValidatePassword(string inputPassword, string storedHashedPassword)
        {
            var hashedInputPassword = HashPassword(inputPassword);
            return hashedInputPassword == storedHashedPassword;
        }


        // Generate JWT token
        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: "sajed",
                audience: "students",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // User login
        [HttpGet]
        public IActionResult LogIn()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogIn([FromBody] LoginViewModel loginDetails)
        {
            var hashedPassword = HashPassword(loginDetails.Password);
            var user = dataHelper.GetData()
                .FirstOrDefault(u =>
                    u.Username == loginDetails.Username && u.Password == hashedPassword && u.IsActive == true);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("IsAdmin", user.IsAdmin.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true // Keep the user logged in
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Json(new
                {
                    success = true,
                    username = user.Username,
                    email = user.Email,
                    isAdmin = user.IsAdmin
                });
            }

            return Json(new { success = false, message = "Invalid username or password." });
        }

        // Reuse the HashPassword method from Signup


        // Admin dashboard
        public IActionResult AdminMain()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("LogIn", "Users"); // Redirect to login if not logged in
            }

            if (!IsAdmin(username))
            {
                return RedirectToAction("Index", "Users"); // Redirect to Home/Index if not admin
            }

            return View();
        }

        [HttpGet]
        public IActionResult ManageUsers()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("LogIn", "Users"); // Redirect to login if not logged in
            }

            if (!IsAdmin(username))
            {
                return RedirectToAction("Index", "Users"); // Redirect to Home/Index if not admin
            }

            var users = dataHelper.GetData(); // Replace with your user data retrieval logic
            return View(users); // Goes to ManageUsers.cshtml
        }

        // Helper method to check if the user is an admin
        private bool IsAdmin(string username)
        {
            // Fetch user data from database using the username
            var user = dataHelper.GetData().FirstOrDefault(u => u.Username == username);
            return user != null && user.IsAdmin;
        }


        [HttpPost]
        public IActionResult CreateUser([FromForm] User newUser)
        {
            try
            {
                if (dataHelper.Find(newUser.Username) != null)
                {
                    return Json(new { success = false, message = "Username already exists." });
                }

                newUser.Password = HashPassword(newUser.Password); // Hash the password
                dataHelper.Add(newUser);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public IActionResult ToggleActive(string username)
        {
            var user = dataHelper.Find(username);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            dataHelper.Edit(username, user);
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public IActionResult EditUser([FromForm] User editedUser)
        {
            try
            {
                var existingUser = dataHelper.Find(editedUser.Username);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Update fields only if they are not null or empty
                if (!string.IsNullOrWhiteSpace(editedUser.Email))
                {
                    existingUser.Email = editedUser.Email;
                }

                if (!string.IsNullOrWhiteSpace(editedUser.Gender))
                {
                    existingUser.Gender = editedUser.Gender;
                }

                // IsAdmin and IsActive are boolean; update directly
                existingUser.IsAdmin = editedUser.IsAdmin;
                existingUser.IsActive = editedUser.IsActive;

                dataHelper.Edit(editedUser.Username, existingUser);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = dataHelper.GetData().FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Message = "Email not found.";
                return View();
            }

            // Generate a reset token
            string token = Guid.NewGuid().ToString();

            // Create a reset request and save it
            var resetRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Token = token,
                ExpiryDate = DateTime.Now.AddHours(1)
            };

            resetRequestHelper.Add(resetRequest);

            // Generate the reset link
            string resetLink = Url.Action("ResetPassword", "Users", new { token }, Request.Scheme);

            // Send the reset link via email
            notificationService.SendEmail(
                to: email,
                subject: "Password Reset Request",
                body: $"Click <a href='{resetLink}'>here</a> to reset your password. This link will expire in 1 hour."
            );

            ViewBag.Message = "A password reset link has been sent to your email.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var resetRequest = resetRequestHelper.GetData().Where(r => r.Token == token && r.ExpiryDate > DateTime.Now).FirstOrDefault();
            if (resetRequest == null)
            {
                return View("Error");
            }

            return View(new UserDTOs.ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        public IActionResult ResetPassword(UserDTOs.ResetPasswordViewModel model)
        {
            var resetRequest = resetRequestHelper.GetData().Where(r => r.Token == model.Token && r.ExpiryDate > DateTime.Now).FirstOrDefault();
            if (resetRequest == null)
            {
                return View("Error");
            }

            var user = dataHelper.GetData().FirstOrDefault(u => u.Email == resetRequest.Email);
            if (user == null)
            {
                return View("Error");
            }

            // Update the user's password
            user.Password = HashPassword(model.NewPassword);
            dataHelper.Edit(user.Username, user);

            // Remove the reset request
            resetRequestHelper.Delete(resetRequest.Id);

            return RedirectToAction("LogIn");
        }
    }
}