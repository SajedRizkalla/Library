using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MyLibrary.Data
{
    public class PasswordResetRequestEntity : iDataHelper<PasswordResetRequest>
    {
        private readonly DBContext _context;

        public PasswordResetRequestEntity(DBContext context)
        {
            _context = context;
        }

        // Add a new PasswordResetRequest
        public void Add(PasswordResetRequest table)
        {
            _context.PasswordResetRequests.Add(table);
            _context.SaveChanges(); // Save changes to the database
        }

        // Delete a PasswordResetRequest by ID (string-based ID as per iDataHelper interface)
        public void Delete(string Id)
        {
            var resetRequest = Find(Id);
            if (resetRequest != null)
            {
                _context.PasswordResetRequests.Remove(resetRequest);
                _context.SaveChanges(); // Save changes to the database
            }
        }

        // Update a PasswordResetRequest by ID (string-based ID as per iDataHelper interface)
        public void Edit(string Id, PasswordResetRequest table)
        {
            if (int.TryParse(Id, out int id))
            {
                var resetRequest = Find(Id);
                if (resetRequest != null)
                {
                    resetRequest.Email = table.Email;
                    resetRequest.Token = table.Token;
                    resetRequest.ExpiryDate = table.ExpiryDate;

                    _context.PasswordResetRequests.Update(resetRequest);
                    _context.SaveChanges(); // Save changes to the database
                }
            }
        }

        // Find a PasswordResetRequest by ID (string-based ID as per iDataHelper interface)
        public PasswordResetRequest Find(string Id)
        {
            return _context.PasswordResetRequests.FirstOrDefault(x => x.Id == Id);

            return null;
        }

        // Get all PasswordResetRequests
        public List<PasswordResetRequest> GetData()
        {
            return _context.PasswordResetRequests.ToList(); // Fetch all password reset requests
        }

        // Search PasswordResetRequests based on email or token
        public List<PasswordResetRequest> Search(string SearchItem)
        {
            return _context.PasswordResetRequests.Where(x =>
                x.Email.Contains(SearchItem) ||
                x.Token.Contains(SearchItem)
            ).ToList();
        }
    }
}