using System;
using VDS.RDF;
using VDS.RDF.Query;

namespace AreLobstersSpiders
{
    public static class SparqlResultExtension
    {
        public static bool TryGetValue<T>(this SparqlResult result, string variable, out T value)
            where T : INode
        {
            if (result.TryGetValue(variable, out var node) && node is T tNode)
            {
                value = tNode;
                return true;
            }

            value = default(T);
            return false;
        }
        
        public static T GetValue<T>(this SparqlResult result, string variable)
            where T : INode
        {
            if (result.TryGetValue(variable, out var node) && node is T tNode)
            {
                return tNode;
            }
            throw new InvalidOperationException($"The result does not contain any values for '{variable}'.");
        }
    }
}