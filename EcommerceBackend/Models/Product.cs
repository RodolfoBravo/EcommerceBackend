namespace EcommerceBackend.Models
{
    public class Product
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public decimal price { get; set; }
        public string category { get; set; }
        public List<Image> images { get; set; }
        public bool isAvailable { get; set; }
        public int stock { get; set; }
        public List<Option> options { get; set; }
    }

    public class Category
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class Image
    {
        public int id { get; set; }
        public string url { get; set; }
        public int productId { get; set; }
    }

    public class Option
    {
        public int id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
        public int productId { get; set; }
    }

    public class Comment
    {
        public int id { get; set; }
        public int productId { get; set; }
        public int userId { get; set; }
        public int rating { get; set; }
        public string content { get; set; }
    }
}
