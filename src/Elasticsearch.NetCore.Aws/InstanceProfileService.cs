﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Elasticsearch.NetCore.Aws
{
    internal static class InstanceProfileService
    {
        private static readonly string[] AliasSeparators = { "<br/>" };
        private const string Server = "http://169.254.169.254";
        private const string RolesPath = "/latest/meta-data/iam/security-credentials/";
        private const string SuccessCode = "Success";
        private static InstanceProfileCredentials _cachedCredentials;
        private static readonly object CacheLock = new object();

        internal static InstanceProfileCredentials GetCredentials()
        {
            var cachedCredentials = GetCachedCredentials();
            if (cachedCredentials != null)
                return cachedCredentials;

            lock (CacheLock)
            {
                if (GetCachedCredentials() == null)
                {
                    var role = GetFirstRole();
                    var json = GetContents(new Uri(Server + RolesPath + role));
                    var credentials = JsonConvert.DeserializeObject<InstanceProfileCredentials>(json);  

                    if (credentials.Code != SuccessCode)
                        return null;

                    _cachedCredentials = credentials;

                }
            }

            return GetCachedCredentials();
        }

        private static InstanceProfileCredentials GetCachedCredentials()
        {
            if (_cachedCredentials != null)
            {
                if (_cachedCredentials.Expiration > DateTime.UtcNow.AddMinutes(15))
                {
                    return _cachedCredentials;
                }
            }
            return null;
        }

        private static string GetFirstRole()
        {
            var roles = GetAvailableRoles();
            foreach (var role in roles)
            {
                return role;
            }

            // no roles found
            throw new InvalidOperationException("No roles found");
        }

        private static IEnumerable<string> GetAvailableRoles()
        {
            var allAliases = GetContents(new Uri(Server + RolesPath));
            

            if (string.IsNullOrEmpty(allAliases))
                yield break;

            var parts = allAliases.Split(AliasSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trim = part.Trim();
                if (!string.IsNullOrEmpty(trim))
                    yield return trim;
            }
        }

        private static string GetContents(Uri uri)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = uri;

                    HttpResponseMessage response = client.GetAsync("/posts").Result;
                    response.EnsureSuccessStatusCode();

                    var stringResponse = response.Content.ReadAsStreamAsync().Result;
                    StreamReader str = new StreamReader(stringResponse);
                    return str.ReadToEnd();
                    
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception("Unable to reach credentials server", ex);
                }
            }
        }
    }
}
