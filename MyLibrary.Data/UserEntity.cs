using Microsoft.EntityFrameworkCore;
using MyLibrary.Data;  // Import the correct namespace

namespace MyLibrary.Data
{
    public class UserEntity : iDataHelper<MyLibrary.Data.User>  // Update this line
    {
        private readonly DBContext _context;

        public UserEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(MyLibrary.Data.User table)  // Update type to match the correct User
        {
            _context.Users.Add(table);
            _context.SaveChanges(); // Save changes to the database
        }

        public void Delete(string Id)
        {
            var user = Find(Id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges(); // Save changes to the database
            }
        }

        public void Edit(string Id, MyLibrary.Data.User table)  // Update type to match the correct User
        {
            var user = Find(Id);
            if (user != null)
            {
                user.Username = table.Username;
                user.Email = table.Email;
                user.Password = table.Password;
                user.IsActive = table.IsActive;

                _context.Users.Update(user);
                _context.SaveChanges(); // Save changes to the database
            }
        }

        public MyLibrary.Data.User Find(string Id)  // Update type to match the correct User
        {
            return _context.Users.FirstOrDefault(x => x.Username == Id); // Fetch user from the database
        }

        public List<MyLibrary.Data.User> GetData()  // Update type to match the correct User
        {
            return _context.Users.ToList(); // Fetch all users from the database
        }

        public List<MyLibrary.Data.User> Search(string SearchItem)  // Update type to match the correct User
        {
            return _context.Users.Where(x =>
                x.Username.Contains(SearchItem) ||
                x.Email.Contains(SearchItem) ||
                x.Password.Contains(SearchItem)
            ).ToList(); // Perform search in the database
        }
    }
}
