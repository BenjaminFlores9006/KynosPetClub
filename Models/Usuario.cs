using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KynosPetClub.Models
{
    public class Usuario
    {
        public string nombre { get; set; }
        public string apellido { get; set; }
        public DateTime fechanac { get; set; }
        public string correo { get; set; }
        public string contraseña { get; set; }
    }
}
