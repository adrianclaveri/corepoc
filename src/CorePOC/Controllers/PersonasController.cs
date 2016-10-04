using AutoMapper;
using CorePOC.Models;
using CorePOC.Repositories;
using CorePOC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorePOC.Controllers
{
    [Route("api/[controller]")]
    public class PersonasController : Controller
    {
        public IPersonaRepository _personaRepository { get; set; }

        public PersonasController(IPersonaRepository personaRepository)
        {
            _personaRepository = personaRepository;
        }

        // GET api/personas
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var result = _personaRepository.GetAll();

                return Ok(Mapper.Map<ICollection<PersonaViewModel>>(result)); //Maps a collection of ViewModel to Model
            }
            catch
            {
                return BadRequest("Error al traer personas");
            }
        }

        // Post api/personas
        [HttpPost]
        public IActionResult Post([FromBody] PersonaViewModel persona)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var newPersona = Mapper.Map<Persona>(persona);
                    _personaRepository.Add(newPersona);

                    return Created($"api/personas/", Mapper.Map<PersonaViewModel>(newPersona));
                }
                return BadRequest("Error en el modelo");
            }
            catch
            {
                return BadRequest("Error al crear personas");
            }
        }
    }
}
