using System.ComponentModel.DataAnnotations;

namespace API_produit.Models
{
    public class Produit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du produit est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Le prix est obligatoire.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix doit être supérieur à 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Le stock est obligatoire.")]
        [Range(0, int.MaxValue, ErrorMessage = "Le stock doit être un nombre positif.")]
        public int Stock { get; set; }
    }
}
