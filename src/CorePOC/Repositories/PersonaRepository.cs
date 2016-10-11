using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorePOC.Models;
using Microsoft.Extensions.Configuration;
using Nest;
using Elasticsearch.NetCore.Aws;
using Elasticsearch.Net;

namespace CorePOC.Repositories
{
    public class PersonaRepository : IPersonaRepository
    {
        //private List<Persona> _db = new List<Persona>();
        public IConfigurationRoot _config { get; set; }
        public ElasticClient client { get; set; }

        public PersonaRepository(IConfigurationRoot config)
        {
            _config = config;

            var httpConnection = new AwsHttpConnection(new AwsSettings
            {
                AccessKey = "AKIAI63FYLJHOUAV7HQA",
                SecretKey = "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",
                Region = "us-east-1",
            });

            var indexName = _config["ConnectionStrings:IndexName"];
            var node = new Uri("https://search-bunee-test-hxpcuz56nhqazfwlwz75wtexh4.us-east-1.es.amazonaws.com");
            var pool = new SingleNodeConnectionPool(node);
            var settings = new ConnectionSettings(pool, httpConnection).DefaultIndex(indexName);
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
