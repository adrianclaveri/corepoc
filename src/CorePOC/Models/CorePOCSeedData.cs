using Elasticsearch.Net;
using Elasticsearch.NetCore.Aws;
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
            catch
            {
                Console.WriteLine("Error al conectar a BD");
            }

        }
    }
}
