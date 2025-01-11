using Microsoft.AspNetCore.Mvc;
using MyLibrary.Data;
using System;
using System.Linq;
using MyLibrary.Data.DTOS;
using MyLibrary.Service.Services;
namespace MyLibrary.Controllers
{
    public class BorrowController : Controller
    {
        private readonly iDataHelper<Book> bookHelper;
        private readonly iDataHelper<User> userHelper;
        private readonly iDataHelper<RentedRecord> rentedHelper;
        private readonly NotificationService notificationService;
        private readonly iDataHelper<WaitingList> waitingListHelper;

        public BorrowController(iDataHelper<Book> b, iDataHelper<User> u,  iDataHelper<WaitingList> wl,iDataHelper<RentedRecord> r, NotificationService n)
        {
            bookHelper = b;
            userHelper = u;
            rentedHelper = r;
            notificationService = n;
            waitingListHelper = wl;

        }

        /// <summary>
        /// Borrow a book. If the book has RentQuantity>0, create a RentedRecord for 30 days.
        /// If no copies left, user is placed in waiting list.
        /// </summary>
        // [HttpPost]
        public IActionResult BorrowBook([FromBody] RentDTOs.BorrowRequest request)
        {
            var book = bookHelper.Find(request.BookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            var user = userHelper.Find(request.Username);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (book.IsJustForSell)
                return BadRequest(new { message = "Book is buy-only and cannot be borrowed." });

            if (book.RentQuantity > 0)
            {
                // We have a copy available
                book.RentQuantity--;
                bookHelper.Edit(book.Id, book);
                

                var record = new RentedRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = book.Id,
                    Username = user.Username,
                    BorrowDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(30),
                    ReminderSent = false
                };
                rentedHelper.Add(record);

                notificationService.SendEmail(
                    to: user.Email,
                    subject: "Book Borrowed",
                    body: $"You have successfully borrowed '{book.Title}' until {record.DueDate:yyyy-MM-dd}."
                );

                return Ok(new { message = "Book borrowed successfully." });
            }
            else
            {
                // No copies => waiting list
                bookHelper.Edit(book.Id, book);

                notificationService.SendEmail(
                    to: user.Email,
                    subject: "Added to Waiting List",
                    body: $"All copies of '{book.Title}' are out. You've been added to the waiting list."
                );

                return Ok(new { message = "All copies in use. Added to waiting list." });
            }
        }

        [HttpPost]
        [Route("Borrow/ReturnBook")]
        public IActionResult ReturnBook([FromBody] RentDTOs.ReturnBookRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.BookId) || string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest("Invalid request payload.");
                }

                var book = bookHelper.Find(request.BookId);
                if (book == null) return NotFound("Book not found.");

                // Increase rentQuantity
                book.RentQuantity++;
                bookHelper.Edit(request.BookId, book);

                // Find the user's RentedRecord
                var record = rentedHelper.GetData()
                    .FirstOrDefault(r => r.BookId == request.BookId && r.Username == request.Username);

                var waitingListOrder = waitingListHelper.GetData()
                    .Where(r => r.BookId == request.BookId) // Filter by the specific BookId
                    .OrderBy(r => r.AddedDate)             // Order by AddedDate (earliest first)
                    .FirstOrDefault();  
                if (record != null)
                {
                    rentedHelper.Delete(record.Id);
                }

                // Notify the next user in the waiting list
                if (waitingListOrder != null)
                {
                    var nextUserUsername = waitingListOrder.Username;
                    // bookHelper.Edit(request.BookId, book);

                    var nextUser = userHelper.Find(nextUserUsername);
                    if (nextUser != null)
                    {
                        notificationService.SendEmail(
                            to: nextUser.Email,
                            subject: "Book Available!",
                            body: $"The book '{book.Title}' is now available for borrowing."
                        );
                    }
                    waitingListHelper.Delete(waitingListOrder.Id);
                }

                return Ok(new { success = true, message = "Book returned successfully." });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReturnBook: {ex.Message}");
                return StatusCode(500, "An error occurred while returning the book.");
            }
        }


        [HttpPost]
        public IActionResult CheckReminders()
        {
            var now = DateTime.Now;
            var records = rentedHelper.GetData();

            foreach (var record in records)
            {
                var daysLeft = (record.DueDate - now).TotalDays;
                if (daysLeft <= 5 && !record.ReminderSent)
                {
                    var user = userHelper.Find(record.Username);
                    if (user != null)
                    {
                        notificationService.SendEmail(
                            to: user.Email,
                            subject: "Reminder: Book Due Soon",
                            body: $"Dear {record.Username}, your borrowed book is due in {daysLeft:F0} days. Please return it on time."
                        );

                        record.ReminderSent = true;
                        rentedHelper.Edit(record.Id, record);
                    }
                }
            }

            return Ok("Reminders sent.");
        }
        [HttpPost]
        public IActionResult NotifyDue([FromBody] RentDTOs.NotifyDueRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.BookId) || string.IsNullOrEmpty(request.Username))
                return BadRequest("Invalid request payload.");

            var record = rentedHelper.GetData()
                .FirstOrDefault(r => r.BookId == request.BookId && r.Username == request.Username);

            if (record == null)
                return NotFound("Rented record not found.");

            var user = userHelper.Find(request.Username);
            if (user == null)
                return NotFound("User not found.");

            var daysLeft = (record.DueDate - DateTime.Now).TotalDays;
            if (daysLeft > 5)
                return BadRequest("This user still has more than 5 days left. No reminder needed.");

            // Send the email reminder
            notificationService.SendEmail(
                to: user.Email,
                subject: "Reminder: Book is due soon",
                body: $"Dear {user.Username}, your borrowed book '{record.BookId}' is due in {daysLeft:F0} days."
            );

            return Ok("Reminder sent successfully.");
        }
       
    }

   
}  