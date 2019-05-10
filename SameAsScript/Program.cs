using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Query;
using System.Linq;
using System.IO;

namespace SameAsScript
{
    class Program
    {
        private object _consoleLock = new object();

        private object _predicatesSetLock = new object();
        public Dictionary<KeyValuePair<Uri, string>, int> PredicatesDictionary { get; set; }

        private object _countLock = new object();
        private int _count;
        public int Count
        {
            get { lock (_countLock) return _count; }
            set { lock (_countLock) _count = value; }
        }

        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("usage: dotnet run <output_file>");
                return;
            }
            new Program().Run(args[0]);
        }

	    public string Path { get; private set; }

        void Run(string path)
        {
	    Path = path;
            ThreadPool.SetMinThreads(100, 100);
            using (File.Create(path)) {}
            File.Delete(path);
            PredicatesDictionary = new Dictionary<KeyValuePair<Uri, string>, int>();
            Console.CancelKeyPress += (s, e) => Save();
            Query(new Uri("http://purl.obolibrary.org/obo/NCBITaxon_2759"),
                (subject, label) => Predicates(label));
        }

        private object _saveLock = new object();
        private void Save()
	    {
                using (var writer = new StreamWriter(File.Open(Path, FileMode.Create)))
                {
                    foreach (var v in PredicatesDictionary.OrderByDescending(pair => pair.Value))
                    {
                        writer.WriteLine($"{v.Value} {v.Key.Key} {v.Key.Value}");
                    }
                }
	    }

        private int _tasksCount = 0;
        private object _tasksCountLock = new object();

        void Query(Uri uri, Action<Uri, string> callback, int indent = 0)
        {
            // 134.209.17.65
            var endpoint = new SparqlRemoteEndpoint(new Uri("http://fuseki:3030/species/query"));
            var results = endpoint.QueryWithResultSet(@"
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
select distinct ?subject ?label where {
  ?subject rdfs:subClassOf <" + uri + @"> .
  ?subject rdfs:label ?label .
}
");

            var childs = new List<Uri>();
            Parallel.ForEach(results, result => {
                lock (_tasksCountLock) _tasksCount++;
                if (result.TryGetValue("subject", out var subjectNode) && subjectNode is UriNode uriNode &&
                    result.TryGetValue("label", out var labelNode) && labelNode is LiteralNode literalNode)
                {
                    var subject = uriNode.Uri;
                    var label = literalNode.Value;
                    callback(subject, label);
                    lock (_consoleLock)
                    {
                    	lock (_tasksCountLock) Console.Write($"tasks: {_tasksCount:00000} ");
                        Console.Write($"uris: {Count++:0000000}\r");
                    }
                    childs.Add(subject);
		    if (Count%100 == 0)
		    	lock (_saveLock)
				Save();
                }
                lock (_tasksCountLock) _tasksCount--;
            });
            foreach (var subject in childs)
            {
                Query(subject, callback, indent+1);
            }
        }

        void Predicates(string name)
        {
            var lowerName = name.ToLower();
            var names = new List<string>();
            names.Add($"\"{lowerName}\"");
            var upperNameSb = new StringBuilder($"\"{lowerName}\"");
            upperNameSb[1] = (char)(upperNameSb[1] + 'A' - 'a');
            names.Add(upperNameSb.ToString());

            var types = new List<string>
            {
                "^^rdf:langString",
                "@en",
                "@fr"
            };

            var endpoint = new SparqlRemoteEndpoint(new Uri("http://dbpedia.org/sparql/"));
            var queryFormat = "select ?subject ?predicate where {{ ?subject ?predicate {0} . }}";
            
            foreach (var n in names)
            {
                foreach (var t in types)
                {
                    var query = String.Format(queryFormat, $"{n}{t}");
                    try
                    {
                        var results = endpoint.QueryWithResultSet(query);
                        foreach (var result in results)
                        {
                            if (result.TryGetValue("predicate", out var predicateNode) && predicateNode is UriNode uriNode)
                            {
                                var predicate = uriNode.Uri;
                                var pair = new KeyValuePair<Uri, string>(predicate, t);
                                lock (_predicatesSetLock)
                                {
                                    if (!PredicatesDictionary.ContainsKey(pair))
                                        PredicatesDictionary.Add(pair, 1);
                                    else
                                        PredicatesDictionary[pair]++;
                                }
                            }
                        }
                    }
                    catch (Exception) {}
                }
            }
        }
    }
}
