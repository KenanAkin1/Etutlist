using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Etutlist.Models
{
    public class Mazeret
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Personel seçmek zorunludur")]
        public int PersonelId { get; set; }

        [ValidateNever] // Navigation property validasyona girmesin
        public Personel Personel { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Baslangic { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Bitis { get; set; }


    }
}
