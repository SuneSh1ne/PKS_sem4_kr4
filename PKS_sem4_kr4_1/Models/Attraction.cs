using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TouristGuide.Models
{
    public class Attraction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string History { get; set; }

        [StringLength(500)]
        public string PhotoPath { get; set; }

        [StringLength(200)]
        public string WorkingHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EntryFee { get; set; }

        // Внешний ключ
        public int CityId { get; set; }

        // Навигационное свойство
        public City City { get; set; }
    }
}