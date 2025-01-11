using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyLibrary.Data
{
    public class RentedRecordEntity : iDataHelper<RentedRecord>
    {
        private readonly DBContext _context;

        public RentedRecordEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(RentedRecord table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "RentedRecord cannot be null.");

            _context.RentedRecords.Add(table);
            _context.SaveChanges();
        }

        public void Delete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("RentedRecord ID cannot be null or empty.", nameof(Id));

            var record = Find(Id);
            if (record == null)
                throw new KeyNotFoundException($"RentedRecord with ID '{Id}' not found.");

            _context.RentedRecords.Remove(record);
            _context.SaveChanges();
        }

        public void Edit(string Id, RentedRecord table)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("RentedRecord ID cannot be null or empty.", nameof(Id));
            if (table == null)
                throw new ArgumentNullException(nameof(table), "RentedRecord cannot be null.");

            var existingRecord = Find(Id);
            if (existingRecord == null)
                throw new KeyNotFoundException($"RentedRecord with ID '{Id}' not found.");

            // Update fields
            existingRecord.BookId = table.BookId;
            existingRecord.Username = table.Username;
            existingRecord.BorrowDate = table.BorrowDate;
            existingRecord.DueDate = table.DueDate;
            existingRecord.ReminderSent = table.ReminderSent;

            _context.RentedRecords.Update(existingRecord);
            _context.SaveChanges();
        }

        public RentedRecord Find(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("RentedRecord ID cannot be null or empty.", nameof(Id));

            // If you want to return null instead of throwing, you can do:
            // return _context.RentedRecords.FirstOrDefault(x => x.Id == Id);
            // else if you want to throw if not found:
            return _context.RentedRecords.FirstOrDefault(x => x.Id == Id)
                ?? throw new KeyNotFoundException($"RentedRecord with ID '{Id}' not found.");
        }

        public List<RentedRecord> GetData()
        {
            return _context.RentedRecords.ToList();
        }

        // If you want to implement search, adapt to the fields in RentedRecord
        public List<RentedRecord> Search(string SearchItem)
        {
            if (string.IsNullOrWhiteSpace(SearchItem))
                throw new ArgumentException("Search item cannot be null or empty.", nameof(SearchItem));

            SearchItem = SearchItem.Trim();

            // Example: searching by BookId, Username, or partial match in ID
            var query = _context.RentedRecords.Where(x =>
                x.Id.Contains(SearchItem) ||
                x.BookId.Contains(SearchItem) ||
                x.Username.Contains(SearchItem)
            );

            return query.ToList();
        }
    }
}
