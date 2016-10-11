using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Globalization;

namespace Elasticsearch.NetCore.Aws
{
    public static class SignV4Util
    {
        static readonly char[] _datePartSplitChars = { 'T' };
        
        public static void SignRequest(HttpRequestMessage request, byte[] body, Credentials credentials, string region, string service)
        {
            //var date=DateTime.ParseExact("20110909T233600Z", "yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            //var amzDate = "20110909T233600Z";

            var date = DateTime.UtcNow;
            var dateStamp = date.ToString("yyyyMMdd");
            var amzDate = date.ToString("yyyyMMddTHHmmssZ");
            request.Headers.Add("X-Amz-Date", amzDate);


            var signingKey = GetSigningKey(credentials.SecretKey, dateStamp, region, service);
            var stringToSign = GetStringToSign(request, body, region, service);
            Debug.Write("========== String to Sign ==========\r\n{0}\r\n========== String to Sign ==========\r\n", stringToSign);
            var signature = signingKey.GetHmacSha256Hash(stringToSign).ToLowercaseHex();
            var auth = string.Format(
                "Credential={0}/{1}, SignedHeaders={2}, Signature={3}",
                credentials.AccessKey,
                GetCredentialScope(dateStamp, region, service),
                GetSignedHeaders(request),
                signature);

            request.Headers.Authorization  = new System.Net.Http.Headers.AuthenticationHeaderValue("AWS4-HMAC-SHA256", auth);
            if (!string.IsNullOrWhiteSpace(credentials.Token))
                request.Headers.Add("x-amz-security-token", credentials.Token);
        }

        public static byte[] GetSigningKey(string secretKey, string dateStamp, string region, string service)
        {
            return _encoding.GetBytes("AWS4" + secretKey)
                .GetHmacSha256Hash(dateStamp)
                .GetHmacSha256Hash(region)
                .GetHmacSha256Hash(service)
                .GetHmacSha256Hash("aws4_request");
        }

        private static byte[] GetHmacSha256Hash(this byte[] key, string data)
        {
            using (var kha = new HMACSHA256(key))
            {
                return kha.ComputeHash(_encoding.GetBytes(data));
            }
        }

        public static string GetStringToSign(HttpRequestMessage request, byte[] data, string region, string service)
        {
            var canonicalRequest = GetCanonicalRequest(request, data);
            Debug.Write("========== Canonical Request ==========\r\n{0}\r\n========== Canonical Request ==========\r\n", canonicalRequest);
            string awsDate = request.Headers.GetValues("x-amz-date").FirstOrDefault();

            Debug.Assert(Regex.IsMatch(awsDate, @"\d{8}T\d{6}Z"));
            var datePart = awsDate.ToString().Split(_datePartSplitChars, 2)[0];
            return string.Join("\n",
                "AWS4-HMAC-SHA256",
                awsDate,
                GetCredentialScope(datePart, region, service),
                GetHash(canonicalRequest).ToLowercaseHex()
            );
        }

        private static string GetCredentialScope(string date, string region, string service)
        {
            return string.Format("{0}/{1}/{2}/aws4_request", date, region, service);
        }

        public static string GetCanonicalRequest(HttpRequestMessage request, byte[] data)
        {
            Uri requestUri = request.RequestUri;
            
            var canonicalHeaders = request.GetCanonicalHeaders();
            var result = new StringBuilder();
            result.Append(request.Method);
            result.Append('\n');
            result.Append(GetPath(request.RequestUri));
            result.Append('\n');
            result.Append(requestUri.GetCanonicalQueryString());
            result.Append('\n');
            WriteCanonicalHeaders(canonicalHeaders, result);
            result.Append('\n');
            WriteSignedHeaders(canonicalHeaders, result);
            result.Append('\n');
            WriteRequestPayloadHash(data, result);
            return result.ToString();
        }

        private static string GetPath(Uri uri)
        {
            var path = uri.AbsolutePath;
            if (path.Length == 0) return "/";

            IEnumerable<string> segments = path
                .Split('/')
                .Select(segment =>
                {
                    string escaped = Uri.EscapeDataString(segment);
                    escaped = escaped.Replace("*", "%2A");
                    return escaped;
                }
                );
            return string.Join("/", segments);
        }
        public static string GetCanonicalQueryString(this Uri uri)
        {
            if (string.IsNullOrWhiteSpace(uri.Query)) return string.Empty;
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            List<string> queryP= queryParams.Keys.ToList();
            queryP.Sort();

            var output = new StringBuilder();
            foreach (var elm in queryP)
            {
                if (output.Length > 0) output.Append('&');
                output.WriteEncoded(elm);
                output.Append('=');
                output.WriteEncoded(queryParams[elm].ToString());
            }
            return output.ToString();
        }

        private static void WriteEncoded(this StringBuilder output, string value)
        {
            for (var i = 0; i < value.Length; ++i)
            {
                if (value[i].RequiresEncoding())
                {
                    output.Append(Uri.EscapeDataString(value[i].ToString()));
                }
                else
                {
                    output.Append(value[i]);
                }
            }
        }

        private static bool RequiresEncoding(this char value)
        {
            if ('A' <= value && value <= 'Z') return false;
            if ('a' <= value && value <= 'z') return false;
            if ('0' <= value && value <= '9') return false;
            switch (value)
            {
                case '-':
                case '_':
                case '.':
                case '~':
                    return false;
            }
            return true;
        }

        private static Dictionary<string, string> GetCanonicalHeaders(this HttpRequestMessage request)
        {
            Dictionary<string, string> result= new Dictionary<string, string>();

            foreach (KeyValuePair<string,IEnumerable<string>> keyvalue in request.Headers)
            {
                result.Add(keyvalue.Key.ToLowerInvariant(), keyvalue.Value.FirstOrDefault());
            }

            result["content-type"] = request.Content.Headers.ContentType.ToString();
            result["host"] = request.RequestUri.Host.ToLowerInvariant();
            return result;
        }

        private static void WriteCanonicalHeaders(Dictionary<string, string> canonicalHeaders, StringBuilder output)
        {
            var q = from pair in canonicalHeaders
                    orderby pair.Key ascending
                    select string.Format("{0}:{1}\n", pair.Key, pair.Value);
            foreach (var line in q)
            {
                output.Append(line);
            }
        }

        private static string GetSignedHeaders(HttpRequestMessage request)
        {
            var canonicalHeaders = request.GetCanonicalHeaders();
            var result = new StringBuilder();
            WriteSignedHeaders(canonicalHeaders, result);
            return result.ToString();
        }

        private static void WriteSignedHeaders(Dictionary<string, string> canonicalHeaders, StringBuilder output)
        {
            bool started = false;
            foreach (var pair in canonicalHeaders.OrderBy(v => v.Key))
            {
                if (started) output.Append(';');
                output.Append(pair.Key.ToLowerInvariant());
                started = true;
            }
        }

        static readonly byte[] _emptyBytes = new byte[0];

        private static void WriteRequestPayloadHash(byte[] data, StringBuilder output)
        {
            data = data ?? _emptyBytes;
            var hash = GetHash(data);
            foreach (var b in hash)
            {
                output.AppendFormat("{0:x2}", b);
            }
        }

        private static string ToLowercaseHex(this byte[] data)
        {
            var result = new StringBuilder();
            foreach (var b in data)
            {
                result.AppendFormat("{0:x2}", b);
            }
            return result.ToString();
        }

        static readonly UTF8Encoding _encoding = new UTF8Encoding(false);

        private static byte[] GetHash(string data)
        {
            return GetHash(_encoding.GetBytes(data));
        }

        private static byte[] GetHash(this byte[] data)
        {
            using (var kha = SHA256.Create())
            {
                return kha.ComputeHash(data);
            }
        }
        
        public static Uri ToUri(this HttpRequest request)
        {
            var hostComponents = request.Host.ToUriComponent().Split(':');

            var builder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = hostComponents[0],
                Path = request.Path,
                Query = request.QueryString.ToUriComponent()
            };

            if (hostComponents.Length == 2)
            {
                builder.Port = Convert.ToInt32(hostComponents[1]);
            }

            return builder.Uri;
        }
        public static Uri GetRawUrl(this HttpRequest request)
        {
            var httpContext = request.HttpContext;

            var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();

            return new Uri(requestFeature.RawTarget);
        }
    }
}
