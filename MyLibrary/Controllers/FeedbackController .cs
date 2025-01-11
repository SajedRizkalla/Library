using Microsoft.AspNetCore.Mvc;
using MyLibrary.Data;

namespace MyLibrary.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly DBContext context;

        public FeedbackController(DBContext context)
        {
            this.context = context;
        }

        [HttpPost]
        public IActionResult SubmitFeedback(RatingDTO ratingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            // Create the Rating object
            var rating = new Rating
            {
                RatingValue = ratingDto.RatingValue,
                Feedback = ratingDto.Feedback,
                BookId = ratingDto.BookId,
                IsWebsiteRating = false,
                Timestamp = DateTime.UtcNow
            };

            // Add the rating to the Ratings table
            context.Ratings.Add(rating);

            // Find the corresponding book
            var book = context.Books.FirstOrDefault(b => b.Id == ratingDto.BookId);
            if (book != null)
            {
                // Add the new rating to the RatingList
                var updatedRatingList = book.RatingList;
                updatedRatingList.Add(rating);
                book.RatingList = updatedRatingList; // Updates RatingListJson internally

                // Mark the RatingListJson property as modified
                context.Entry(book).Property(b => b.RatingListJson).IsModified = true;

                // Save changes to the database
                try
                {
                    Console.WriteLine($"Serialized RatingListJson: {book.RatingListJson}");
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving changes: {ex.Message}");
                    throw;
                }
            }

            // Redirect back to the MyBooks page
            return RedirectToAction("MyBooks", "Users", new { id = ratingDto.BookId });
        }

        [HttpPost]
        public IActionResult WebSubmitFeedback(RatingDTO ratingDto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("LogIn", "Users");
            
            if (!ModelState.IsValid || (ratingDto.RatingValue == 0 && string.IsNullOrWhiteSpace(ratingDto.Feedback)))
            {
                return BadRequest("Please provide a valid rating or feedback.");
            }


            // Create and add the Rating object
            var rating = new Rating
            {
                RatingValue = ratingDto.RatingValue,
                Feedback = ratingDto.Feedback,
                IsWebsiteRating = true,
                Timestamp = DateTime.UtcNow
            };

            context.Ratings.Add(rating);
            context.SaveChanges();

            // Redirect to Index to refresh the page
            return RedirectToAction("Index", "Users");
        }


    }
}
