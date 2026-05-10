using System.ComponentModel.DataAnnotations;

namespace PKS_sem4_kr4_2.Models
{
    public class ProductionLine
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название линии обязательно")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // "Active" или "Stopped"

        // Коэффициент эффективности (0.5 - 2.0)
        public float EfficiencyFactor { get; set; } = 1.0f;

        // ID текущего выполняемого заказа
        public int? CurrentWorkOrderId { get; set; }

        // Навигационные свойства
        public WorkOrder? CurrentWorkOrder { get; set; }
        public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}