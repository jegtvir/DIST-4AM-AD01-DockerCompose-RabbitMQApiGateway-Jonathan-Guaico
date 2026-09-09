using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Categoria.Api.Models
{
    [Table("categoria")]
    public class Categorias
    {
        [Key]
        public int Id_categoria { get; set; }
        [StringLength(50, ErrorMessage = "El nombre de la categoría no puede exceder los 50 caracteres.")]
        public string nombre_categoria { get; set; } = string.Empty;
        [StringLength(250, ErrorMessage = "La descripción de la categoría no puede exceder los 250 caracteres.")]
        public string descripcion_categoria { get; set; } = string.Empty;
        [Required]
        public bool Estado_categoria { get; set; }
    }

}
