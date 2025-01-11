using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyLibrary.Data
{
    public class CartItemEntity : iDataHelper<CartItem>
    {
        private readonly DBContext _context;

        public CartItemEntity(DBContext context)
        {
            _context = context;
        }

        public void Add(CartItem table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "CartItem cannot be null.");

            _context.CartItems.Add(table);
            _context.SaveChanges();
        }

        public void Delete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("CartItem ID cannot be null or empty.", nameof(Id));

            var item = Find(Id);
            if (item == null)
                throw new KeyNotFoundException($"CartItem with ID '{Id}' not found.");

            _context.CartItems.Remove(item);
            _context.SaveChanges();
        }

        public void Edit(string Id, CartItem table)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("CartItem ID cannot be null or empty.", nameof(Id));
            if (table == null)
                throw new ArgumentNullException(nameof(table), "CartItem cannot be null.");

            var existingItem = Find(Id);
            if (existingItem == null)
                throw new KeyNotFoundException($"CartItem with ID '{Id}' not found.");

            // Update relevant fields
            existingItem.BookId   = table.BookId;
            existingItem.Username = table.Username;
            existingItem.IsForRent = table.IsForRent;
            existingItem.BuyQuantity = table.BuyQuantity;
            existingItem.RentDays = table.RentDays;
            existingItem.DateAdded = table.DateAdded;

            _context.CartItems.Update(existingItem);
            _context.SaveChanges();
        }

        public CartItem Find(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("CartItem ID cannot be null or empty.", nameof(Id));

            return _context.CartItems
                .FirstOrDefault(x => x.Id == Id)
                ?? throw new KeyNotFoundException($"CartItem with ID '{Id}' not found.");
        }

        public List<CartItem> GetData()
        {
            return _context.CartItems.ToList();
        }

        public List<CartItem> Search(string SearchItem)
        {
            if (string.IsNullOrWhiteSpace(SearchItem))
                throw new ArgumentException("Search item cannot be null or empty.", nameof(SearchItem));

            SearchItem = SearchItem.Trim();

            var query = _context.CartItems.Where(x =>
                x.Id.Contains(SearchItem) ||
                x.BookId.Contains(SearchItem) ||
                x.Username.Contains(SearchItem)
            );

            return query.ToList();
        }
    }
}
