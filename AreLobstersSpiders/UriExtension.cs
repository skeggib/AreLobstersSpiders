using System;
using System.Text;
using VDS.RDF;
using VDS.RDF.Query;

namespace AreLobstersSpiders
{
    public static class UriExtension
    {
        public static string GetLiteralValue(this Uri uri, Uri property, Uri endPointUri)
            => GetResultValue(uri, property, endPointUri).GetValue<LiteralNode>("value").Value;
            
        public static Uri GetUriValue(this Uri uri, Uri property, Uri endPointUri)
            => GetResultValue(uri, property, endPointUri).GetValue<UriNode>("value").Uri;
        
        public static SparqlResult GetResultValue(this Uri uri, Uri property, Uri endPointUri)
        {
            var endPoint = new SparqlRemoteEndpoint(endPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine("select ?value where {");
            querySb.AppendLine($"    <{uri}> <{property}> ?value .");
            querySb.AppendLine("} limit 1");
            var query = querySb.ToString();
            return endPoint.QueryWithResultSet(query)[0];
        }

        public static string Label(this Uri uri, Uri endPointUri)
            => uri.GetLiteralValue(Prefixes.GetUri("rdfs", "label"), endPointUri);

        public static string Rank(this Uri uri, Uri endPointUri)
        {
            var endpoint = new SparqlRemoteEndpoint(endPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine(Prefixes.GetSparqlPrefix("rdfs"));
            querySb.AppendLine($"select ?rank");
            querySb.AppendLine($"where {{");
            querySb.AppendLine($"  <{uri}> <http://purl.obolibrary.org/obo/ncbitaxon#has_rank> ?r .");
            querySb.AppendLine($"  ?r rdfs:label ?rank .");
            querySb.AppendLine($"}}");
            var results = endpoint.QueryWithResultSet(querySb.ToString());

            if (results.Count != 1)
                throw new Exception("Unexpected results.");

            results[0].TryGetValue("rank", out var node);
            if (!(node is LiteralNode literalNode))
                throw new Exception("Unexpected results.");
            return literalNode.Value;
        }
    }
}