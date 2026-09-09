using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vehiculo.Api.Models
{
    [Table("vehiculos")]
    public class Vehiculos
    {
        [Key]
        [Column("Id_vehiculo")]
        public int Id_vehiculo { get; set; }
        [Column("Id_categoria")]
        public int Id_categoria { get; set; }
        [Column("marca_vehiculo")]
        [StringLength(50,ErrorMessage = "La marca del vehículo debe tener como máximo 50 caracteres")]
        public string marca_vehiculo { get; set; } = string.Empty;
        [Column("modelo_vehiculo")]
        [StringLength(50,ErrorMessage = "El modelo del vehículo debe tener como máximo 50 caracteres")]
        public string modelo_vehiculo { get; set; } = string.Empty;
        [Column("precio_vehiculo")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio del vehículo debe ser un valor positivo")]
        public decimal precio_vehiculo { get; set; }
        [Column("stock_vehiculo")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock del vehículo debe ser un valor positivo")]
        public int stock_vehiculo { get; set; }
        [Column("estado_vehiculo")]
        public bool estado_vehiculo { get; set; }
    }
}
