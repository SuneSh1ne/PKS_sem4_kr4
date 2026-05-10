using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PKS_sem4_kr4_2.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название продукта обязательно")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // Храним характеристики в формате JSON строки
        public string? SpecificationsJson { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int MinimalStock { get; set; } = 10;

        public int ProductionTimePerUnit { get; set; } = 10; // минуты на единицу

        // Навигационные свойства
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
        public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

        // Вспомогательное свойство для работы с JSON
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Dictionary<string, string>? Specifications
        {
            get
            {
                if (string.IsNullOrEmpty(SpecificationsJson))
                    return new Dictionary<string, string>();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(SpecificationsJson);
            }
            set
            {
                SpecificationsJson = JsonSerializer.Serialize(value);
            }
        }
    }
}