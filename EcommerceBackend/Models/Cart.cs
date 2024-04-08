namespace EcommerceBackend.Models
{
    public class CartItem
    {
        public int id { get; set; }
        public int productId { get; set; }
        public int quantity { get; set; }
    }

    public class Cart
    {
        public int id { get; set; }
        public int userId { get; set; }
        public List<CartItem> items { get; set; } = new List<CartItem>();
    }
}
