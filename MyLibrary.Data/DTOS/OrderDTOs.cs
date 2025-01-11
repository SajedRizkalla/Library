namespace MyLibrary.Data.DTOS;

public class OrderDTOs
{
    public class WaitingListDTO
    {
        public string BookId { get; set; }
        public string Username { get; set; }
        public string BookTitle { get; set; }
    }
    
    public class DeletePurchasedBookDTO
    {
        public string BookId { get; set; }
    }
    
    public class CheckoutCartItemDto
    {
        public string BookId { get; set; }
        public string Title { get; set; }

        public bool IsForRent { get; set; }

        /// <summary>
        /// Quantity of books if the user is buying.
        /// </summary>
        public int BuyQuantity { get; set; }

        /// <summary>
        /// Number of days if the user is renting.
        /// </summary>
        public int RentDays { get; set; }

        /// <summary>
        /// The price of a single unit (per day if renting, per copy if buying).
        /// This might already factor in a discount if the book is on sale.
        /// </summary>
        public float PricePerUnit { get; set; }

        /// <summary>
        /// The final cost for this item, based on BuyQuantity or RentDays.
        /// </summary>
        public float FinalPrice 
        {
            get
            {
                if (IsForRent)
                {
                    // If renting, final price can be calculated as:
                    // PricePerUnit * RentDays
                    return PricePerUnit ;
                }
                else
                {
                    // If buying, final price can be:
                    // PricePerUnit * BuyQuantity
                    return PricePerUnit * BuyQuantity;
                }
            }
        }
    }
}