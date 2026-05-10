namespace PKS_sem4_kr4_2.Models
{
    public class ProductMaterial
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        // Количество материала, необходимое для 1 единицы продукта
        public decimal QuantityNeeded { get; set; }
    }
}