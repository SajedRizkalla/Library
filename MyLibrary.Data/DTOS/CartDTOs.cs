namespace MyLibrary.Data.DTOS;

public class CartDTOs
{
    public class CartItemViewModel
    {
        // CartItem fields
        public string Id { get; set; }
        public string Username { get; set; }
        public string BookId { get; set; }
        public bool IsForRent { get; set; }
        public int BuyQuantity { get; set; }
        public int RentDays { get; set; }
        public DateTime DateAdded { get; set; }

        // Book fields (fetched via BookId)
        public string Title { get; set; }
        public string Author { get; set; }
        // etc. (Publisher, Genre, Price, etc.) if you want them
    }
}