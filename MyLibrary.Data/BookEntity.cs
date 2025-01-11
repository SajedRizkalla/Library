using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLibrary.Data;

namespace MyLibrary.Data
{
    public class BookEntity : iDataHelper<Book>
    {
        private readonly DBContext _context;

        public BookEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(Book table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Book cannot be null.");

            _context.Books.Add(table);
            _context.SaveChanges(); // Save changes to the database
        }

        public void Delete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Book ID cannot be null or empty.", nameof(Id));

            var book = Find(Id);
            if (book == null)
                throw new KeyNotFoundException($"Book with ID '{Id}' not found.");

            _context.Books.Remove(book);
            _context.SaveChanges(); // Save changes to the database
        }

        public void Edit(string Id, Book table)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Book ID cannot be null or empty.", nameof(Id));
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Book cannot be null.");

            var book = Find(Id);
            if (book == null)
                throw new KeyNotFoundException($"Book with ID '{Id}' not found.");

            // Update fields
            book.Cover = table.Cover;
            book.Author = table.Author;
            book.Title = table.Title;
            book.Publisher = table.Publisher;
            book.Borrowprice = table.Borrowprice;
            book.Buyprice = table.Buyprice;
            book.Year = table.Year;
            book.SalePercentage = table.SalePercentage;
            book.DownloadUrl = table.DownloadUrl;
            book.SaleEndDate = table.SaleEndDate;
            book.RentQuantity = table.RentQuantity;
            book.IsJustForSell = table.IsJustForSell;

            _context.Books.Update(book);
            _context.SaveChanges(); // Save changes to the database
        }

        public Book Find(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Book ID cannot be null or empty.", nameof(Id));

            return _context.Books.FirstOrDefault(x => x.Id == Id)
                ?? throw new KeyNotFoundException($"Book with ID '{Id}' not found.");
        }

        public List<Book> GetData()
        {
            return _context.Books.ToList();
        }

        public List<Book> Search(string SearchItem)
        {
            if (string.IsNullOrWhiteSpace(SearchItem))
                throw new ArgumentException("Search item cannot be null or empty.", nameof(SearchItem));

            // Trim the SearchItem to remove leading and trailing whitespaces
            SearchItem = SearchItem.Trim();

            // Attempt to parse SearchItem to a float for numeric comparisons
            bool isNumericSearch = float.TryParse(SearchItem, out float searchValue);

            // Define a small tolerance for floating-point comparisons
            const float tolerance = 0.0001f;

            // Query the books
            var query = _context.Books.Where(x =>
                x.Id.Contains(SearchItem) ||
                x.Cover.Contains(SearchItem) ||
                x.Author.Contains(SearchItem) ||
                x.Title.Contains(SearchItem) ||
                x.Publisher.Contains(SearchItem) ||
                (isNumericSearch && (
                    Math.Abs(x.Borrowprice - searchValue) < tolerance ||
                    Math.Abs(x.Buyprice - searchValue) < tolerance ||
                    Math.Abs(x.Year - searchValue) < tolerance ||
                    Math.Abs(x.SalePercentage - searchValue) < tolerance
                ))
            );

            return query.ToList();
        }

    }
}
