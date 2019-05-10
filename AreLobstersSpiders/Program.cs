using System;
using System.Linq;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Builder;
using System.Text;

namespace AreLobstersSpiders
{
    class Program
    {
        static string Usage => @"usage:
    dotnet AreLobsterSpiders.dll search word            Search an URI by label
    dotnet AreLobsterSpiders.dll isa    URI_a URI_b     Checks if URI_a is a URI_b
    dotnet AreLobsterSpiders.dll taxon  DBpedia_uri     Gets taxons of a DBpedia URI";

        static Uri NCBITaxonEndPointUri => new Uri("http://134.209.17.65:3030/species/query");

        static Uri DBpediaEndPointUri => new Uri("https://dbpedia.org/sparql");

        static HashSet<(Uri Property, string Type)> _dbpediaTaxonProperties = new HashSet<(Uri Property, string Type)>
        {
            (new Uri("http://dbpedia.org/property/taxon"), "^^rdf:langString"),
            (new Uri("http://www.w3.org/2000/01/rdf-schema#label"), "@en"),
            (new Uri("http://www.w3.org/2000/01/rdf-schema#label"), "@fr"),
            (new Uri("http://dbpedia.org/property/name"), "^^rdf:langString"),
            (new Uri("http://xmlns.com/foaf/0.1/name"), "@en"),
            (new Uri("http://dbpedia.org/property/title"), "^^rdf:langString"),
            (new Uri("http://xmlns.com/foaf/0.1/givenName"), "@en"),
            (new Uri("http://dbpedia.org/property/binomial"), "^^rdf:langString"),
            (new Uri("http://dbpedia.org/property/species"), "^^rdf:langString"),
            (new Uri("http://dbpedia.org/property/genus"), "^^rdf:langString"),
            (new Uri("http://dbpedia.org/property/familia"), "^^rdf:langString"),
            (new Uri("http://dbpedia.org/property/subdivision"), "^^rdf:langString"),
        };

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(Usage);
                return;
            }

            switch (args[0])
            {
                case "search":
                    SearchCommand(args.Skip(1));
                    break;
                case "isa":
                    IsACommand(args.Skip(1));
                    break;
                case "taxon":
                    TaxonCommand(args.Skip(1));
                    break;
                default:
                    Console.WriteLine(Usage);
                    break;
            }
        }

        static void SearchCommand(IEnumerable<string> args)
        {
            if (args.Count() < 1)
            {
                Console.WriteLine(Usage);
                return;
            }
            foreach (var pair in Search.QuickSearch(args.First(), NCBITaxonEndPointUri))
                Console.WriteLine($"'{pair.Value}' <{pair.Key}>");
        }

        static void IsACommand(IEnumerable<string> args)
        {
            if (args.Count() < 2)
            {
                Console.WriteLine(Usage);
                return;
            }

            var argsList = args.ToList();
            
            var dbpediaUriA = new Uri(argsList[0]);
            if (!Exists(dbpediaUriA, DBpediaEndPointUri))
            {
                Console.WriteLine("The URI " + dbpediaUriA + " does not exists.");
                return;
            }

            var dbpediaUriB = new Uri(argsList[1]);
            if (!Exists(dbpediaUriB, DBpediaEndPointUri))
            {
                Console.WriteLine("The URI " + dbpediaUriB + " does not exists.");
                return;
            }

            var ncbiUrisA = NCBITaxonUris(dbpediaUriA);
            if (ncbiUrisA.Count <= 0)
            {
                Console.WriteLine("No taxon found for " + dbpediaUriA);
                return;
            }

            var ncbiUrisB = NCBITaxonUris(dbpediaUriB);
            if (ncbiUrisB.Count <= 0)
            {
                Console.WriteLine("No taxon found for " + dbpediaUriB);
                return;
            }

            var ncbiUriA = ncbiUrisA.First();
            var ncbiUriB = ncbiUrisB.First();

            Console.WriteLine(dbpediaUriA + " -> " + ncbiUriA + " (" + ncbiUriA.Label(NCBITaxonEndPointUri) + ")");
            Console.WriteLine(dbpediaUriB + " -> " + ncbiUriB + " (" + ncbiUriB.Label(NCBITaxonEndPointUri) + ")");

            var lcs = LeastCommonSuperClass(ncbiUriA, ncbiUriB, NCBITaxonEndPointUri);
            var lcsLabel = lcs.Label(NCBITaxonEndPointUri);

            Console.WriteLine("Least common superclass: " + lcs + " (" + lcsLabel + ")");

            string formatForDisplay(object str) => str.ToString().ToLower().Replace("_", " ").Replace("-", " ");

            var nameA = formatForDisplay(dbpediaUriA.ToString().Split('/').Last());
            var nameB = formatForDisplay(dbpediaUriB.ToString().Split('/').Last());
            var nameLsc = formatForDisplay(lcsLabel);
            var rank = formatForDisplay(lcs.Rank(NCBITaxonEndPointUri));
            
            Console.WriteLine();
            if (ncbiUriB.Equals(lcs))
            {
                Console.WriteLine($"A{(nameA[0].IsVowel() ? "n" : "")} {nameA} is a{(nameB[0].IsVowel() ? "n" : "")} {nameB}.");
            }
            else
            {
                Console.WriteLine($"A{(nameA[0].IsVowel() ? "n" : "")} {nameA} is not a{(nameB[0].IsVowel() ? "n" : "")} {nameB} but they are both in the {nameLsc} {rank}.");
            }
        }

        static void TaxonCommand(IEnumerable<string> args)
        {
            if (args.Count() < 1)
            {
                Console.WriteLine(Usage);
                return;
            }
            var uri = new Uri(args.First());
            if (!Exists(uri, DBpediaEndPointUri))
            {
                Console.WriteLine("This URI does not exists.");
                return;
            }
            var ncbiUris = NCBITaxonUris(uri);
            if (ncbiUris.Count <= 0)
            {
                Console.WriteLine("No taxon found.");
                return;
            }
            foreach (var ncbiUri in ncbiUris)
            {
                Console.WriteLine(ncbiUri);
            }
        }

        static bool Exists(Uri uri, Uri endPoint)
        {
            var endpoint = new SparqlRemoteEndpoint(endPoint);
            var querySb = new StringBuilder();
            querySb.AppendLine("select *");
            querySb.AppendLine("where {");
            querySb.AppendLine($"    <{uri.ToString()}> ?p ?o .");
            querySb.AppendLine("} limit 1");
            var results = endpoint.QueryWithResultSet(querySb.ToString());
            return results.Count() >= 1;
        }

        static HashSet<Uri> NCBITaxonUris(Uri dbpediaUri)
        {
            var uris = new HashSet<Uri>();
            var taxons = Taxons(dbpediaUri);
            foreach (var taxon in taxons)
            {
                foreach (var uri in Search.QuickSearch(taxon, NCBITaxonEndPointUri).Select(pair => pair.Key))
                {
                    uris.Add(uri);
                }
            }
            return uris;
        }

        static HashSet<string> Taxons(Uri dbpediaUri)
        {
            var endpoint = new SparqlRemoteEndpoint(DBpediaEndPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine("select ?taxon");
            querySb.AppendLine("where {{");
            querySb.AppendLine($"    <{dbpediaUri.ToString()}> <{{0}}> ?taxon .");
            querySb.AppendLine("}}");
            var queryFormat = querySb.ToString();
            var taxons = new HashSet<string>();
            foreach (var taxonProperty in _dbpediaTaxonProperties)
            {
                var query = string.Format(queryFormat, taxonProperty.Property);
                var results = endpoint.QueryWithResultSet(query);
                foreach (var result in results)
                {
                    if (result.TryGetValue<LiteralNode>("taxon", out var node))
                    {
                        taxons.Add(node.Value);
                    }
                }
            }
            return taxons;
        }

        /// <summary>
        /// Searches for the least common super-class of two nodes a and b (the
        /// closest node c to a and b such as a and b are both an
        /// rdfs:subClassOf* of c).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="endPointUri">The SPARQL end-point.</param>
        /// <param name="label">The label of the least common super-class.</param>
        /// <returns>The least common super-class.</returns>
        static Uri LeastCommonSuperClass(Uri a, Uri b, Uri endPointUri)
        {
            var endpoint = new SparqlRemoteEndpoint(endPointUri);
            var querySb = new StringBuilder();
            querySb.AppendLine(Prefixes.GetSparqlPrefix("rdfs"));
            querySb.AppendLine($"select ?lcs ?label");
            querySb.AppendLine($"where {{");
            querySb.AppendLine($"  ?lcs ^rdfs:subClassOf*");
            querySb.AppendLine($"      <{a.ToString()}> ,");
            querySb.AppendLine($"      <{b.ToString()}> .");
            querySb.AppendLine($"  filter not exists {{");
            querySb.AppendLine($"   	?sublcs ^rdfs:subClassOf*");
            querySb.AppendLine($"        <{a.ToString()}> ,");
            querySb.AppendLine($"      	 <{b.ToString()}> .");
            querySb.AppendLine($"    ?sublcs rdfs:subClassOf ?lcs;");
            querySb.AppendLine($"  }}");
            querySb.AppendLine($"}}");
            var results = endpoint.QueryWithResultSet(querySb.ToString());

            if (results.Count != 1)
                throw new Exception("Unexpected results.");

            if (!results[0].TryGetValue<UriNode>("lcs", out var lcsNode))
                throw new Exception("Unexpected results.");
            return lcsNode.Uri;
        }
    }
}
