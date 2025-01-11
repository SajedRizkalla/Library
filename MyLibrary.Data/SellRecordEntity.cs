using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyLibrary.Data
{
    public class SellRecordEntity : iDataHelper<SellRecord>
    {
        private readonly DBContext _context;

        public SellRecordEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(SellRecord table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "SellRecord cannot be null.");

            _context.SellRecords.Add(table);
            _context.SaveChanges();
        }

        public void Delete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("SellRecord ID cannot be null or empty.", nameof(Id));

            var record = Find(Id);
            if (record == null)
                throw new KeyNotFoundException($"SellRecord with ID '{Id}' not found.");

            _context.SellRecords.Remove(record);
            _context.SaveChanges();
        }

        public void Edit(string Id, SellRecord table)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("SellRecord ID cannot be null or empty.", nameof(Id));
            if (table == null)
                throw new ArgumentNullException(nameof(table), "SellRecord cannot be null.");

            var existingRecord = Find(Id);
            if (existingRecord == null)
                throw new KeyNotFoundException($"SellRecord with ID '{Id}' not found.");

            // Update fields
            existingRecord.BookId       = table.BookId;
            existingRecord.Username     = table.Username;
            existingRecord.PurchaseDate = table.PurchaseDate;
            existingRecord.Quantity     = table.Quantity;
            existingRecord.UnitPrice    = table.UnitPrice;

            _context.SellRecords.Update(existingRecord);
            _context.SaveChanges();
        }

        public SellRecord Find(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("SellRecord ID cannot be null or empty.", nameof(Id));

            return _context.SellRecords
                .FirstOrDefault(x => x.Id == Id)
                ?? throw new KeyNotFoundException($"SellRecord with ID '{Id}' not found.");
        }

        public List<SellRecord> GetData()
        {
            return _context.SellRecords.ToList();
        }

        public List<SellRecord> Search(string SearchItem)
        {
            if (string.IsNullOrWhiteSpace(SearchItem))
                throw new ArgumentException("Search item cannot be null or empty.", nameof(SearchItem));

            SearchItem = SearchItem.Trim();

            var query = _context.SellRecords.Where(x =>
                x.Id.Contains(SearchItem) ||
                x.BookId.Contains(SearchItem) ||
                x.Username.Contains(SearchItem)
            );

            return query.ToList();
        }
    }
}
