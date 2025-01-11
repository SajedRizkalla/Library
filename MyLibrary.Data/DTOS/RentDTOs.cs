namespace MyLibrary.Data.DTOS;

public class RentDTOs
{
    public class ReturnBookRequest
    {
        public string BookId { get; set; }
        public string Username { get; set; }
    }
    
    public class NotifyDueRequest
    {
        public string BookId { get; set; }
        public string Username { get; set; }
    }
    
    public class BorrowRequest
    {
        public string BookId { get; set; }
        public string Username { get; set; }
    }
}