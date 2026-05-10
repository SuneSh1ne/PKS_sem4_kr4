using System.ComponentModel.DataAnnotations;

namespace TouristGuide.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Region { get; set; }

        public int Population { get; set; }

        [StringLength(2000)]
        public string History { get; set; }

        [StringLength(500)]
        public string CoatOfArmsPath { get; set; } // путь к гербу

        [StringLength(500)]
        public string PhotoPath { get; set; } // путь к фото

        // Навигационное свойство
        public ICollection<Attraction> Attractions { get; set; } = new List<Attraction>();
    }
}