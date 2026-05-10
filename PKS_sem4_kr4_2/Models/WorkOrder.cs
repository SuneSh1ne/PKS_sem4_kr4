using System.ComponentModel.DataAnnotations;

namespace PKS_sem4_kr4_2.Models
{
    public class WorkOrder
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? ProductionLineId { get; set; }
        public ProductionLine? ProductionLine { get; set; }

        public int Quantity { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EstimatedEndDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; 
        // "Pending" - ожидает
        // "InProgress" - в процессе
        // "Completed" - завершён
        // "Cancelled" - отменён

        public int ProgressPercent { get; set; } = 0; // 0-100

        public DateTime? ActualEndDate { get; set; }
    }
}