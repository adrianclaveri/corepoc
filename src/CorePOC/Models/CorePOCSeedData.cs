using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorePOC.Models
{
    public class CorePOCSeedData
    {
        public IConfigurationRoot _config { get; set; }

        public CorePOCSeedData(IConfigurationRoot config)
        {
            _config = config;
        }

        public async Task SeedData()
        {
            try
            {
                var indexName = _config["ConnectionStrings:IndexName"];
                Uri node = new Uri(_config["ConnectionStrings:ElasticDockerURL"]);
                ConnectionSettings settings = new ConnectionSettings(node).DefaultIndex(indexName);
                ElasticClient client = new ElasticClient(settings);

                var result = client.TypeExists(indexName, typeof(Persona));
                if (!result.Exists)
                {
                    var indexSettings = new IndexSettings();
                    indexSettings.NumberOfReplicas = 1;
                    indexSettings.NumberOfShards = 1;

                    var indexState = new IndexState();
                    indexState.Settings = indexSettings;

                    await client.CreateIndexAsync(indexName, f => f.InitializeUsing(indexState));
                }
            }
            catch {
                Console.WriteLine("Error al conectar a BD");
            }
        }
    }
}
