using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Query;

namespace AreLobstersSpiders
{
    public static class Search
    {
        
        /// <summary>
        /// Searches for nodes that have an rdfs:label containing a given string.
        /// </summary>
        /// <param name="word">The string the label has to contain.</param>
        /// <param name="endPointUri">The SPARQL end-point.</param>
        /// <returns></returns>
        public static List<KeyValuePair<Uri, string>> ContainsSearch(string word, Uri endPointUri)
        {
            var endpoint = new SparqlRemoteEndpoint(endPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine(Prefixes.GetSparqlPrefix("rdfs"));
            querySb.AppendLine($"select ?subject ?label");
            querySb.AppendLine($"where {{");
            querySb.AppendLine($"    ?subject rdfs:label ?label");
            querySb.AppendLine($"    filter contains(lcase(str(?label)), '{word.ToLower()}')");
            querySb.AppendLine($"}}");
            var results = endpoint.QueryWithResultSet(querySb.ToString());
            var list = new List<KeyValuePair<Uri, string>>();
            foreach (var result in results)
            {
                if (result.TryGetValue<UriNode>("subject", out var subjectNode) &&
                    result.TryGetValue<LiteralNode>("label", out var labelNode))
                {
                    list.Add(new KeyValuePair<Uri, string>(subjectNode.Uri, labelNode.Value));
                }
            }
            return list;
        }

        public static List<KeyValuePair<Uri, string>> QuickSearch(string word, Uri endPointUri)
        {
            var set = new HashSet<KeyValuePair<Uri, string>>();
            set.AddRange(StaticSearch(word, endPointUri));
            set.AddRange(StaticSearch(word.ToLower(), endPointUri));
            var sb = new StringBuilder(word.ToLower());
            sb[0] = (char)(sb[0] + ('A' - 'a'));
            set.AddRange(StaticSearch(sb.ToString(), endPointUri));
            return set.ToList();
        }

        public static List<KeyValuePair<Uri, string>> StaticSearch(string word, Uri endPointUri)
        {
            var endpoint = new SparqlRemoteEndpoint(endPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine(Prefixes.GetSparqlPrefix("rdfs"));
            querySb.AppendLine($"select ?subject");
            querySb.AppendLine($"where {{");
            querySb.AppendLine($"    ?subject rdfs:label \"{word.Replace("\r", "").Replace("\n", "")}\"");
            querySb.AppendLine($"}}");
            var query = querySb.ToString();
            var results = endpoint.QueryWithResultSet(query);
            var list = new List<KeyValuePair<Uri, string>>();
            foreach (var result in results)
            {
                if (result.TryGetValue<UriNode>("subject", out var subjectNode))
                {
                    list.Add(new KeyValuePair<Uri, string>(subjectNode.Uri, word));
                }
            }
            return list;
        }

    }
}