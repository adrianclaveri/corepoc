using CorePOC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorePOC.Repositories
{
    public interface IPersonaRepository
    {
        IEnumerable<Persona> GetAll();
        void Add(Persona persona);
    }
}
