using System.ComponentModel.DataAnnotations;

namespace ABCapoAPi.Data
{
    public class Brand
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public List<Product> Products { get; set; } = new();
    }
}
