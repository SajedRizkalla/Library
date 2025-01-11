using Microsoft.AspNetCore.Mvc;
using MyLibrary.Data;
using System;
using System.Linq;
using MyLibrary.Data.DTOS;

namespace MyLibrary.Controllers
{
    public class CartController : Controller
    {
        private readonly iDataHelper<CartItem> cartHelper;
        private readonly iDataHelper<Book> bookHelper;

        public CartController(iDataHelper<CartItem> c, iDataHelper<Book> b)
        {
            cartHelper = c;
            bookHelper = b;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("LogIn", "Users");

            // 1) Get all CartItems for this user
            var cartItems = cartHelper.GetData()
                .Where(x => x.Username == username)
                .ToList();

            // 2) For each cart item, fetch the matching Book
            var itemsWithBook = cartItems.Select(ci =>
            {
                var book = bookHelper.GetData().FirstOrDefault(b => b.Id == ci.BookId);

                return new CartDTOs.CartItemViewModel
                {
                    // CartItem fields
                    Id          = ci.Id,
                    Username    = ci.Username,
                    BookId      = ci.BookId,
                    IsForRent   = ci.IsForRent,
                    BuyQuantity = ci.BuyQuantity,
                    RentDays    = ci.RentDays,
                    DateAdded   = ci.DateAdded,

                    // Book fields
                    Title  = book?.Title,
                    Author = book?.Author
                    // You can add more if you need (Publisher, Genre, etc.)
                };
            }).ToList();

            // 3) Pass the ViewModel list to the Cart view
            return View(itemsWithBook);
        }

        [HttpPost]
        public IActionResult AddToCart(string bookId, bool isForRent, int quantityOrDays)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not logged in." });

            // This is optional. If you prefer to do the check at final checkout, skip this part:
            if (isForRent)
            {
                // Count how many active "rent" items in cart already
                // (or you can also check how many RentedRecords not returned yet)
                var currentCartRentCount = cartHelper.GetData()
                    .Count(ci => ci.Username == username && ci.IsForRent);

                if (currentCartRentCount >= 3)
                {
                    return Json(new { success = false, message = "You already have 3 rent items in the cart." });
                }
            }

            var cartItem = new CartItem
            {
                Id = Guid.NewGuid().ToString(),
                BookId = bookId,
                Username = username,
                IsForRent = isForRent
            };

            if (isForRent)
            {
                cartItem.RentDays = quantityOrDays;
                cartItem.BuyQuantity = 0;
            }
            else
            {
                cartItem.BuyQuantity = quantityOrDays;
                cartItem.RentDays = 0;
            }

            cartHelper.Add(cartItem);

            return Json(new { success = true, message = "Item added to cart" });
        }


        [HttpPost]
        public IActionResult RemoveCartItem([FromBody] RemoveRequest request)
        {
            try
            {
                cartHelper.Delete(request.CartItemId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class RemoveRequest
        {
            public string CartItemId { get; set; }
        }
    }
}
