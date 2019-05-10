using System;
using System.Collections.Generic;

namespace AreLobstersSpiders
{
    public static class Prefixes
    {
        public static Dictionary<string, string> Dictionary = new Dictionary<string, string>()
        {
            { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
            { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#" }
        };

        public static string GetSparqlPrefix(string name)
            => $"prefix {name}: <{Dictionary[name]}>";

        public static Uri GetUri(string prefixName, string nodeName)
            => new Uri($"{Dictionary[prefixName]}{nodeName}");
    }
}