using Microsoft.EntityFrameworkCore;
using MyLibrary.Data;

public class BookDataHelper : iDataHelper<Book>
{
    private readonly DBContext _context;

    public BookDataHelper(DBContext context)
    {
        _context = context;
    }

    public List<Book> GetData()
    {
        // Fetch all books from the database
        return _context.Books.ToList();
    }

    public List<Book> Search(string SearchItem)
    {
        return _context.Books
            .Where(b => b.Title.Contains(SearchItem) || b.Author.Contains(SearchItem))
            .ToList();
    }

    public Book Find(string Id)
    {
        return _context.Books.Find(Id);
    }

    public void Add(Book table)
    {
        _context.Books.Add(table);
        _context.SaveChanges();
    }

    public void Edit(string Id, Book table)
    {
        var existingBook = _context.Books.Find(Id);
        if (existingBook != null)
        {
            _context.Entry(existingBook).CurrentValues.SetValues(table);
            _context.SaveChanges();
        }
    }

    public void Delete(string Id)
    {
        var book = _context.Books.Find(Id);
        if (book != null)
        {
            _context.Books.Remove(book);
            _context.SaveChanges();
        }
    }
}
