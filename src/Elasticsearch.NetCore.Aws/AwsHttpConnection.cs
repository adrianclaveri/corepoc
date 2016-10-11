using Elasticsearch.Net;
using System;
using System.IO;
using System.Net.Http;

namespace Elasticsearch.NetCore.Aws
{
    public class AwsHttpConnection : HttpConnection
    {
        private Credentials _credentials;
        private readonly string _region;
        private readonly AuthType _authType;

        /// <summary>
        /// Initializes a new instance of the AwsHttpConnection class with the specified AccessKey, SecretKey and Token.
        /// </summary>
        /// <param name="awsSettings">AWS specific settings required for signing requests.</param>
        public AwsHttpConnection(AwsSettings awsSettings)
        {
            if (awsSettings == null) throw new ArgumentNullException("awsSettings");
            if (string.IsNullOrWhiteSpace(awsSettings.Region)) throw new ArgumentException("awsSettings.Region is invalid.", "awsSettings");
            _region = awsSettings.Region.ToLowerInvariant();
            var key = awsSettings.AccessKey;
            var secret = awsSettings.SecretKey;
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(secret))
            {
                _credentials = new Credentials
                {
                    AccessKey = key,
                    SecretKey = secret,
                };
                _authType = AuthType.AccessKey;
            }
            else
            {
                _authType = AuthType.InstanceProfile;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AwsHttpConnection class with credentials from the Instance Profile service
        /// </summary>
        /// <param name="region">AWS region</param>
        public AwsHttpConnection(string region)
            : this(new AwsSettings { Region = region })
        {
        }

        protected override HttpRequestMessage CreateHttpRequestMessage(RequestData requestData)
        {
            if (_authType == AuthType.InstanceProfile)
            {
                RefreshCredentials();
            }
            var request = base.CreateHttpRequestMessage(requestData);
            byte[] data = null;
            if (requestData.PostData != null)
            {
                data = requestData.PostData.WrittenBytes;
                if (data == null)
                {
                    using (var ms = new MemoryStream())
                    {
                        requestData.PostData.Write(ms, requestData.ConnectionSettings);
                        data = ms.ToArray();
                    }
                }
            }
            SignV4Util.SignRequest(request, data, _credentials, _region, "es");
            return request;
        }

        private void RefreshCredentials()
        {
            var credentials = InstanceProfileService.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve session credentials from instance profile service");

            _credentials = new Credentials
            {
                AccessKey = credentials.AccessKeyId,
                SecretKey = credentials.SecretAccessKey,
                Token = credentials.Token,
            };
        }
    }
}
