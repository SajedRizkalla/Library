using Microsoft.AspNetCore.Mvc;
using MyLibrary.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using MyLibrary.Data.DTOS;
using MyLibrary.Service.Services;
using Stripe;

namespace MyLibrary.Controllers
{
    public class OrderController : Controller
    {
        private readonly iDataHelper<CartItem> cartHelper;
        private readonly iDataHelper<User> userHelper;
        private readonly iDataHelper<Book> bookHelper;
        private readonly iDataHelper<RentedRecord> rentedHelper;
        private readonly iDataHelper<SellRecord> sellHelper;
        private readonly iDataHelper<WaitingList> waitingListHelper;
        private readonly NotificationService notificationService;

        public OrderController(
            iDataHelper<CartItem> c,
            iDataHelper<Book> b,
            iDataHelper<RentedRecord> r,
            iDataHelper<SellRecord> s,
            NotificationService n,
            iDataHelper<User> u,
            iDataHelper<WaitingList> wl)
        {
            cartHelper = c;
            bookHelper = b;
            rentedHelper = r;
            sellHelper = s;
            notificationService = n;
            waitingListHelper = wl;
            userHelper = u;
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("LogIn", "Users");

            var cartItems = cartHelper
                .GetData()
                .Where(ci => ci.Username == username)
                .ToList();

            // Convert each cart item to a CheckoutCartItemDto
            var dtoList = new List<OrderDTOs.CheckoutCartItemDto>();

            float totalPrice = 0f; // We'll accumulate total in USD

            foreach (var cartItem in cartItems)
            {
                var book = bookHelper.GetData().FirstOrDefault(b => b.Id == cartItem.BookId);
                if (book == null)
                    continue;

                float basePrice = cartItem.IsForRent ? book.Borrowprice : book.Buyprice;

                if (book.IsOnSale)
                {
                    basePrice -= basePrice * (book.SalePercentage / 100);
                }

                // Create DTO
                var dto = new OrderDTOs.CheckoutCartItemDto
                {
                    BookId = book.Id,
                    Title = book.Title,
                    IsForRent = cartItem.IsForRent,
                    BuyQuantity = cartItem.BuyQuantity,
                    RentDays = cartItem.RentDays,
                    PricePerUnit = basePrice
                };

                dtoList.Add(dto);

                // Accumulate total in USD
                totalPrice += dto.FinalPrice;
            }

            // Pass the DTO list and the total to the view
            ViewBag.TotalPrice = totalPrice; // e.g. 10.0 means $10.00
            return View(dtoList);
        }


        [HttpPost]
        public IActionResult ConfirmCheckout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, messages = new List<string> { "You are not logged in." } });
            }

            var cartItems = cartHelper.GetData()
                .Where(x => x.Username == username)
                .ToList();

            if (!cartItems.Any())
            {
                return Json(new { success = false, messages = new List<string> { "Your cart is empty." } });
            }

            // Count how many active rents does the user have
            var activeRentCount = rentedHelper.GetData()
                .Count(rr => rr.Username == username && rr.IsReturned == false);

            // We'll accumulate messages for each cart item
            var messages = new List<string>();
            bool anySuccess = false;

            foreach (var ci in cartItems)
            {
                var book = bookHelper.Find(ci.BookId);
                if (book == null)
                {
                    messages.Add($"Book with ID {ci.BookId} was not found.");
                    continue;
                }

                // Handle renting
                if (ci.IsForRent)
                {
                    // 1) Check if user already has 3 active rents
                    if (activeRentCount >= 3)
                    {
                        messages.Add($"You cannot rent \"{book.Title}\" because you already have 3 active rentals.");
                        continue;
                    }

                    // 2) Check if book is only for sale
                    if (book.IsJustForSell)
                    {
                        messages.Add($"You cannot rent \"{book.Title}\" because it is only available for sale.");
                        continue;
                    }

                    // 3) Check if rent quantity is available
                    if (book.RentQuantity <= 0)
                    {
                        // Ask user if they want to join the waiting list
                        messages.Add(
                            $"Book ID: {book.Id}, \"{book.Title}\" is now out of stock. Please confirm if you wish to be added to the waiting list.");
                        continue;
                    }

                    // If we get here => success rent
                    book.RentQuantity--;
                    bookHelper.Edit(book.Id, book);

                    var rentRecord = new RentedRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        BookId = book.Id,
                        Username = username,
                        BorrowDate = DateTime.Now,
                        DueDate = DateTime.Now.AddDays(ci.RentDays > 0 ? ci.RentDays : 30),
                        RentPrice = book.Borrowprice,
                        IsReturned = false
                    };
                    rentedHelper.Add(rentRecord);

                    // Increment active rent
                    activeRentCount++;

                    // Remove it from cart
                    cartHelper.Delete(ci.Id);

                    messages.Add($"You have successfully rented \"{book.Title}\".");
                    anySuccess = true;
                }
                else
                {
                    // Handle buying
                    

                    // If we get here => success buy
                    book.SellQuantity -= ci.BuyQuantity;
                    bookHelper.Edit(book.Id, book);

                    var sellRecord = new SellRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        BookId = book.Id,
                        Username = username,
                        PurchaseDate = DateTime.Now,
                        Quantity = ci.BuyQuantity,
                        UnitPrice = book.Buyprice
                    };
                    sellHelper.Add(sellRecord);

                    // Remove it from cart
                    cartHelper.Delete(ci.Id);

                    messages.Add($"You have successfully purchased \"{book.Title}\" (quantity: {ci.BuyQuantity}).");
                    anySuccess = true;
                }
            }

            // ============== SEND EMAIL IF SUCCESS ==============
            if (anySuccess)
            {
                // 1) Retrieve the logged user's email from your user service or ASP.NET Identity
                //    For demonstration, let's assume we have "GetUserEmail" or something similar.
                //    You must implement your own logic to fetch the actual email.
                var userEmail = GetCurrentUserEmail(username);
                // e.g. if you have a user table or using ASP.NET Identity:
                // var appUser = _userManager.FindByNameAsync(username).Result;
                // string userEmail = appUser.Email;

                // 2) Craft your email subject/body. 
                //    Optionally, combine the messages into one text block.
                var combinedMessages = string.Join("\n", messages);

                notificationService.SendEmail(
                    to: userEmail,
                    subject: "Your LibraryOfBooks Checkout Confirmation",
                    body: $"Dear {username},\n\n" +
                          "Thank you for your order! Here are the details:\n\n" +
                          $"{combinedMessages}\n\n" +
                          "We hope you enjoy your books!"
                );
            }

            // Return JSON response to client
            return Json(new
            {
                success = anySuccess,
                messages = messages
            });
        }

// Example placeholder. Replace with your real logic to retrieve user email.
        private string GetCurrentUserEmail(string username)
        {
            var email = userHelper.GetData().FirstOrDefault(u => u.Username == username).Email ??
                        "testuser@someemail.com";
            // Hard-coded for demonstration:
            return email;
        }

        [HttpPost]
        public IActionResult AddToWaitingList([FromBody] OrderDTOs.WaitingListDTO dto)
        {
            try
            {
                var waitingEntry = new WaitingList
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = dto.BookId,
                    Username = dto.Username,
                    AddedDate = DateTime.Now
                };
                waitingListHelper.Add(waitingEntry);

                return Json(new
                    { success = true, message = $"You have been added to the waiting list for \"{dto.BookTitle}\"." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult DeletePurchasedBook([FromBody] OrderDTOs.DeletePurchasedBookDTO dto)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "You are not logged in." });
                }

                var purchasedBook = sellHelper.GetData()
                    .FirstOrDefault(sr => sr.BookId == dto.BookId && sr.Username == username);

                if (purchasedBook == null)
                {
                    return Json(new { success = false, message = "Book not found or does not belong to you." });
                }

                sellHelper.Delete(purchasedBook
                    .Id); // Assuming sellHelper.Delete() deletes the record from the database

                return Json(new { success = true, message = "Book deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult Payment()
        {
            return View();
        }

        // A small DTO that the client will send with the total
        public class PaymentRequestDto
        {
            public float Amount { get; set; } // e.g., 10.00 for $10.00
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentRequestDto request)
        {
            // In production, you might re-check the user’s cart and compute the total again
            // for security. For demonstration, we’ll trust `request.Amount`.

            // Convert dollars to cents for Stripe
            long amountInCents = (long)(request.Amount * 100);

            // Your Stripe Secret Key (test mode)
            var secretKey =
                "sk_test_51QeNRWKCjW1mTVvyvaWgwI3yBQHeoPSrE39nRJ9EAPUSThAJJKkwR5zirYBbniGBV3yoZrW3Fbsst9jgBjjZgQdF00a1Qw0486";
            StripeConfiguration.ApiKey = secretKey;

            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            });

            return Ok(new { clientSecret = paymentIntent.ClientSecret });
        }

        [HttpPost]
        public IActionResult DirectPurchase(string bookId, int quantity)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "User is not logged in." });

            var book = bookHelper.GetData().FirstOrDefault(b => b.Id == bookId);
            if (book == null)
                return Json(new { success = false, message = "Book not found." });

            // Check stock
            if (book.SellQuantity < quantity)
                return Json(new { success = false, message = "Not enough stock for this purchase." });

            // Compute price (account for sale if needed)
            float price = book.Buyprice;
            if (book.IsOnSale)
                price -= price * (book.SalePercentage / 100);

            // Insert SellRecord
            book.SellQuantity -= quantity;
            bookHelper.Edit(book.Id, book);

            var sellRecord = new SellRecord
            {
                Id = Guid.NewGuid().ToString(),
                BookId = bookId,
                Username = username,
                PurchaseDate = DateTime.Now,
                Quantity = quantity,
                UnitPrice = price
            };
            sellHelper.Add(sellRecord);
            
            // Send email notification
            var userEmail = GetCurrentUserEmail(username); // Implement this method to retrieve the email.
            notificationService.SendEmail(
                to: userEmail,
                subject: "Purchase Confirmation",
                body: $"Dear {username},\n\n" +
                      $"You have successfully purchased '{book.Title}' x {quantity}.\n" +
                      $"Total Price: {price * quantity:C}.\n\n" +
                      "Thank you for your purchase!"
            );
            
            return Json(new { success = true, message = $"You have purchased '{book.Title}' x {quantity}." });
        }

        [HttpPost]
        public IActionResult DirectRent(string bookId, int days)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "User is not logged in." });

            var book = bookHelper.GetData().FirstOrDefault(b => b.Id == bookId);
            if (book == null)
                return Json(new { success = false, message = "Book not found." });

            // If book is only for sale, reject
            if (book.IsJustForSell)
                return Json(new { success = false, message = "This book cannot be borrowed." });

            // Check rent quantity
            if (book.RentQuantity <= 0)
            {
                // Notify the user about adding to the waiting list
                return Json(new
                {
                    success = false,
                    message =  $"Book ID: {book.Id}, \"{book.Title}\" is now out of stock. Please confirm if you wish to be added to the waiting list."
                });
            }

            // Compute rent price
            float borrowprice = book.Borrowprice;

            // Decrement RentQuantity
            book.RentQuantity--;
            bookHelper.Edit(book.Id, book);

            // Add to rented records
            var record = new RentedRecord
            {
                Id = Guid.NewGuid().ToString(),
                BookId = bookId,
                Username = username,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(days),
                RentPrice = borrowprice ,
                IsReturned = false
            };
            rentedHelper.Add(record);
            
            // Send email notification
            var userEmail = GetCurrentUserEmail(username); // Implement this method to retrieve the email.
            notificationService.SendEmail(
                to: userEmail,
                subject: "Rent Confirmation",
                body: $"Dear {username},\n\n" +
                      $"You have successfully rented '{book.Title}' for {days} days.\n" +
                      $"Total Rent Price: {borrowprice:C}.\n" +
                      $"Due Date: {DateTime.Now.AddDays(days):d}.\n\n" +
                      "Thank you for using our service!"
            );
            
            return Json(new { success = true, message = $"You have rented '{book.Title}' for {days} days." });
        }

    }
}