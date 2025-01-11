using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyLibrary.Data
{
    public class WaitingListEntity : iDataHelper<WaitingList>
    {
        private readonly DBContext _context;

        public WaitingListEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(WaitingList table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Waiting list entry cannot be null.");

            _context.WaitingLists.Add(table);
            _context.SaveChanges(); // Save changes to the database
        }

        public void Delete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Waiting list entry ID cannot be null or empty.", nameof(Id));

            var entry = Find(Id);
            if (entry == null)
                throw new KeyNotFoundException($"Waiting list entry with ID '{Id}' not found.");

            _context.WaitingLists.Remove(entry);
            _context.SaveChanges(); // Save changes to the database
        }

        public void Edit(string Id, WaitingList table)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Waiting list entry ID cannot be null or empty.", nameof(Id));
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Waiting list entry cannot be null.");

            var entry = Find(Id);
            if (entry == null)
                throw new KeyNotFoundException($"Waiting list entry with ID '{Id}' not found.");

            // Update fields
            entry.BookId = table.BookId;
            entry.Username = table.Username;
            entry.AddedDate = table.AddedDate;

            _context.WaitingLists.Update(entry);
            _context.SaveChanges(); // Save changes to the database
        }

        public WaitingList Find(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("Waiting list entry ID cannot be null or empty.", nameof(Id));

            return _context.WaitingLists.FirstOrDefault(x => x.Id == Id)
                ?? throw new KeyNotFoundException($"Waiting list entry with ID '{Id}' not found.");
        }

        public List<WaitingList> GetData()
        {
            return _context.WaitingLists.ToList();
        }

        public List<WaitingList> Search(string SearchItem)
        {
            if (string.IsNullOrWhiteSpace(SearchItem))
                throw new ArgumentException("Search item cannot be null or empty.", nameof(SearchItem));

            SearchItem = SearchItem.Trim();

            var query = _context.WaitingLists.Where(x =>
                x.Id.Contains(SearchItem) ||
                x.BookId.Contains(SearchItem) ||
                x.Username.Contains(SearchItem)
            );

            return query.ToList();
        }
    }
}
