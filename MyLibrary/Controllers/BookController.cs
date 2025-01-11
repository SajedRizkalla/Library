using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyLibrary.Data;
using static MyLibrary.Data.DTOS.BookDTOs;

namespace MyLibrary.Controllers
{
    public class BookController : Controller
    {
        private readonly iDataHelper<Book> dataHelper;
        private readonly iDataHelper<RentedRecord> rentedRecordHelper;
        private readonly iDataHelper<User> userHelper;
        private readonly iDataHelper<WaitingList> waitingListHelper;
        private readonly DBContext _context;


        public BookController(
            iDataHelper<Book> dataHelper,
            iDataHelper<RentedRecord> rentedRecordHelper,
            iDataHelper<User> userHelper,
            iDataHelper<WaitingList> waitingListHelper, DBContext context) // Inject WaitingListEntity
        {
            this.dataHelper = dataHelper;
            this.rentedRecordHelper = rentedRecordHelper;
            this.userHelper = userHelper;
            this.waitingListHelper = waitingListHelper;
            this._context = context;

        }

        // GET: BookController
        public IActionResult Index()
        {
            return View(dataHelper.GetData());
        }

        [HttpPost]
        public JsonResult EditBookJson([FromForm] Book updatedBook)
        {
            if (updatedBook == null || string.IsNullOrEmpty(updatedBook.Id))
            {
                return Json(null);
            }

            try
            {
                dataHelper.Edit(updatedBook.Id, updatedBook); // Or your custom logic
                return Json(updatedBook);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating book: {ex.Message}");
                return Json(null);
            }
        }

        [HttpGet]
        [Route("Book/Download/{id}")]
        public async Task<IActionResult> DownloadAsync(string id, string format)
        {
            // 1) Find the Book record
            var book = dataHelper.Find(id);
            if (book == null || string.IsNullOrEmpty(book.DownloadUrl))
            {
                return NotFound("Book not found or Download URL missing.");
            }

            var remotePdfUrl = book.DownloadUrl;

            // If user requests PDF => just return the PDF as-is
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return await ReturnRemoteFileAsync(remotePdfUrl, "pdf", book.Title);
            }

            // Our "valid" conversion targets for demonstration
            var validFormats = new HashSet<string> { "epub", "mobi", "f2b" };
            if (!validFormats.Contains(format.ToLower()))
            {
                return BadRequest("Only pdf, epub, mobi, or f2b formats are supported.");
            }

            // 2) Download the remote PDF to a temp folder
            var tempFolder = Path.GetTempPath();
            var tempPdfName = Path.GetRandomFileName() + ".pdf";
            var tempPdfPath = Path.Combine(tempFolder, tempPdfName);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(remotePdfUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        return NotFound($"Remote file not found or inaccessible at {remotePdfUrl}.");
                    }

                    var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                    await System.IO.File.WriteAllBytesAsync(tempPdfPath, pdfBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading PDF: {ex.Message}");
                return StatusCode(500, "Error occurred while downloading the PDF.");
            }

            // 3) If user specifically wants "f2b", we will convert PDF->EPUB, then rename to .f2b
            var calibreFormat = format.Equals("f2b", StringComparison.OrdinalIgnoreCase) ? "epub" : format;
            // That means if user wants f2b, we do "ebook-convert input.pdf -> output.epub"
            // Then rename .epub to .f2b.

            var convertedFileBase = Path.GetRandomFileName();
            // e.g. MyRandom.tmp
            // We'll produce MyRandom.tmp.epub, then rename -> MyRandom.tmp.f2b if needed
            var realCalibreOutput = Path.Combine(tempFolder, $"{convertedFileBase}.{calibreFormat}");
            var finalOutputExtension = format.Equals("f2b", StringComparison.OrdinalIgnoreCase) ? "f2b" : format;
            var finalFileName = Path.Combine(tempFolder, $"{convertedFileBase}.{finalOutputExtension}");

            // Potential MIME types
            var fileMappings = new Dictionary<string, string>
            {
                { "pdf", "application/pdf" },
                { "epub", "application/epub+zip" },
                { "mobi", "application/x-mobipocket-ebook" },
                { "f2b", "application/octet-stream" }
            };
            var mimeType = fileMappings.ContainsKey(format.ToLower())
                ? fileMappings[format.ToLower()]
                : "application/octet-stream";
            var outputFileName = $"{book.Title}.{format.ToLower()}";

            // 4) Invoke Calibre to produce (EPUB, MOBI, or "EPUB then rename to F2B")
            var calibrePath = @"C:\Program Files\Calibre2\ebook-convert.exe";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = calibrePath,
                    // "ebook-convert input.pdf output.epub" or .mobi
                    Arguments = $"\"{tempPdfPath}\" \"{realCalibreOutput}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var stdOut = await process.StandardOutput.ReadToEndAsync();
                    var stdErr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"ebook-convert error: {stdErr}");
                        return StatusCode(500, "Conversion process failed.");
                    }
                }

                // Ensure the converted file is present
                if (!System.IO.File.Exists(realCalibreOutput))
                {
                    return NotFound("Converted file not found after conversion.");
                }

                // 5) If user wants f2b, rename the .epub to .f2b
                if (format.Equals("f2b", StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Move(realCalibreOutput, finalFileName); // rename
                }
                else
                {
                    finalFileName = realCalibreOutput; // no rename needed
                }

                // 6) Read the final file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(finalFileName);

                // 7) Return as download
                return File(fileBytes, mimeType, outputFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting file: {ex.Message}");
                return StatusCode(500, "Error occurred while converting the file.");
            }
            finally
            {
                // 8) Clean up temp files
                try
                {
                    if (System.IO.File.Exists(tempPdfPath))
                        System.IO.File.Delete(tempPdfPath);

                    if (System.IO.File.Exists(realCalibreOutput) &&
                        format.Equals("f2b", StringComparison.OrdinalIgnoreCase))
                        System.IO.File.Delete(realCalibreOutput);

                    if (System.IO.File.Exists(finalFileName))
                        System.IO.File.Delete(finalFileName);
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"Cleanup error: {cleanupEx.Message}");
                }
            }
        }

// Helper method: Return the remote file (PDF) as-is if user requested 'pdf'.
        private async Task<IActionResult> ReturnRemoteFileAsync(string remoteUrl, string format, string bookTitle)
        {
            var fileName = $"{bookTitle}.{format.ToLower()}";
            var mimeType = format.Equals("pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/octet-stream";

            try
            {
                // Configure the HttpClientHandler
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true // enable auto redirect
                };

                // Use HttpClient with the handler
                using (var httpClient = new HttpClient(handler))
                {
                    // Add a User-Agent header
                    httpClient.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36");

                    var response = await httpClient.GetAsync(remoteUrl);

                    // Log the status code for debugging
                    Console.WriteLine($"HTTP status code: {response.StatusCode} ({(int)response.StatusCode})");
                    Console.WriteLine($"Reason phrase: {response.ReasonPhrase}");

                    if (!response.IsSuccessStatusCode)
                    {
                        // Provide more detail in the NotFound message
                        return NotFound(
                            $"Remote file not found or inaccessible at {remoteUrl}. " +
                            $"Status code: {response.StatusCode}");
                    }

                    // Read file content
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    // Return file
                    return File(fileBytes, mimeType, fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching remote PDF: {ex}");
                return StatusCode(500, "Error occurred while retrieving the PDF.");
            }
        }



        [HttpGet]
        public JsonResult GetAllBooksJson()
        {
            try
            {
                var books = dataHelper.GetData(); // Fetch all books using your data helper
                return Json(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching books: {ex.Message}");
                return Json(null); // Return null or an appropriate error response
            }
        }

        [HttpGet]
        [Route("Book/Details/{id}")]
        public JsonResult Details(string id)
        {
            Console.WriteLine($"Fetching details for book ID: {id}");
            try
            {
                var book = dataHelper.Find(id);
                if (book == null)
                {
                    Console.WriteLine("Book not found.");
                    return Json(new { success = false, message = "Book not found" });
                }

                Console.WriteLine($"Book found: {book.Title}");
                return Json(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching book details: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while fetching book details" });
            }
        }


        // GET: BookController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BookController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Book collection)
        {
            try
            {
                dataHelper.Add(collection);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(collection);
            }
        }

        // JSON Endpoint to Add a Book
        [HttpPost]
        public JsonResult AddBookJson([FromForm] Book newBook)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                 return Json(null); 
            }

            if (!IsAdmin(username))
            {
                return Json(null); 
            }
            if (newBook == null)
            {
                return Json(null); // Return null if the book data is invalid
            }

            try
            {
                if (newBook.IsJustForSell)
                {
                    newBook.RentQuantity = 0;
                    newBook.Borrowprice = 0;
                }

                // Save the new book to the database
                dataHelper.Add(newBook);

                // Return the saved book data as JSON
                return Json(newBook);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding book: {ex.Message}");
                return Json(null); // Return null if there was an error
            }
        }

        // GET: BookController/Edit/5
        public ActionResult Edit(string id)
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
            var book = dataHelper.Find(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: BookController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Book collection)
        {
            try
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
                dataHelper.Edit(id, collection);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(collection);
            }
        }

        // GET: BookController/Delete/5
        public ActionResult Delete(string id)
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
            var book = dataHelper.Find(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: BookController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                dataHelper.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }


        [HttpPost]
        public JsonResult DeleteBookJson([FromBody] DeleteRequest request)
        {
            try
            {
                var username = User.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Error occurred while deleting the book" }); // Redirect to login if not logged in
                }

                if (!IsAdmin(username))
                {
                    return Json(new { success = false, message = "Error occurred while deleting the book" }); // Redirect to Home/Index if not admin
                }
                
                if (string.IsNullOrEmpty(request.Id))
                {
                    return Json(new { success = false, message = "Invalid book ID" });
                }

                var book = dataHelper.Find(request.Id);
                if (book == null)
                {
                    return Json(new { success = false, message = "Book not found" });
                }

                dataHelper.Delete(request.Id);
                return Json(new { success = true, message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting book: {ex.Message}");
                return Json(new { success = false, message = "Error occurred while deleting the book" });
            }
        }
        
        
        private bool IsAdmin(string username)
        {
            // Fetch user data from database using the username
            var user = userHelper.GetData().FirstOrDefault(u => u.Username == username);
            return user != null && user.IsAdmin;
        }
        [HttpGet]
        public IActionResult ManageRentedBooks()
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
            var allRents = rentedRecordHelper.GetData();
            return View(allRents);
        }
        [HttpGet]
        public JsonResult GetWaitingList(string bookId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Not Authorized." }); // Redirect to login if not logged in
            }

            if (!IsAdmin(username))
            {
                return Json(new { success = false, message = "Not Authorized." }); // Redirect to Home/Index if not admin
            }
            var waitingListEntries = waitingListHelper.Search(bookId);

            if (!waitingListEntries.Any())
            {
                return Json(new { success = false, message = "No users in the waiting list." });
            }

            var waitingListUsers = waitingListEntries.Select(entry =>
            {
                var user = userHelper.Find(entry.Username);
                return user == null
                    ? null
                    : new
                    {
                        Username = user.Username,
                        Email = user.Email,
                        Gender = user.Gender,
                        IsActive = user.IsActive,
                        AddedDate = entry.AddedDate
                    };
            }).Where(x => x != null).ToList();

            return Json(new { success = true, data = waitingListUsers });
        }

       



    }
}