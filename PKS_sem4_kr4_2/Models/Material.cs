using System.ComponentModel.DataAnnotations;

namespace PKS_sem4_kr4_2.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название материала обязательно")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        [StringLength(20)]
        public string UnitOfMeasure { get; set; } = "шт"; // "кг", "шт", "литр" и т.д.

        public decimal MinimalStock { get; set; } = 10;

        // Навигационное свойство
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}