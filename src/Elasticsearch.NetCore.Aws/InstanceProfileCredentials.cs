using System;
using System.Runtime.Serialization;

namespace Elasticsearch.NetCore.Aws
{
    [DataContract]
    internal class InstanceProfileCredentials
    {
        [DataMember]
        public string Code { get; set; }
        [DataMember]
        public DateTime LastUpdated { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string AccessKeyId { get; set; }
        [DataMember]
        public string SecretAccessKey { get; set; }
        [DataMember]
        public string Token { get; set; }
        [DataMember]
        public DateTime Expiration { get; set; }
    }
}
