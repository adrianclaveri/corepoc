using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorePOC.Models;
using Microsoft.Extensions.Configuration;
using Nest;

namespace CorePOC.Repositories
{
    public class PersonaRepository : IPersonaRepository
    {
        //private List<Persona> _db = new List<Persona>();
        public IConfigurationRoot _config { get; set; }
        public Uri node { get; set; }
        public ConnectionSettings settings { get; set; }
        public ElasticClient client { get; set; }

        public PersonaRepository(IConfigurationRoot config)
        {
            _config = config;
            node = new Uri(_config["ConnectionStrings:ElasticDockerURL"]);
            settings = new ConnectionSettings(node).DefaultIndex(_config["ConnectionStrings:IndexName"]);
            client = new ElasticClient(settings);
        }

        public void Add(Persona persona)
        {
            //_db.Add(persona);
            var resp = client.Index(persona);
            if (resp.Created == false && resp.ServerError != null)
                throw new Exception(resp.ServerError.Error.ToString());
        }

        public IEnumerable<Persona> GetAll()
        {
            var hits = client.Search<Persona>(x =>
            x.Query(q => q
            .MultiMatch(mp => mp
            .Query("")
            .Fields(f => f
                .Fields(f1 => f1.Nombre, f2 => f2.Apellido, f3 => f3.Edad)))));

            var results = hits.Documents.ToList();

            return results;
        }
    }
}
