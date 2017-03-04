using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace WordNetEngine
{
    public class WordNet
    {
        private GraphProcessor _dataProcessor;
        private bool _isInitialized;
        private DAG DataGraph { get; set; }
        public WordNet(string graphInputFile,string synsetsFile)
        {
           DataGraph = new DAG(synsetsFile,graphInputFile);
        }

        public WordNet(string graphInputFile, string synsetsFile,Action<int,int> progAction)
        {
            DataGraph = new DAG(synsetsFile, graphInputFile,progAction);
        }
        /// <summary>
        /// Initialize WordNet
        /// </summary>
        public void InitWordNet(bool validation = true)
        {
            _isInitialized = true;
            _dataProcessor = new GraphProcessor(DataGraph,validation);
        }
        //DEBUG FUNCTION
        //TODO REMOVE
        public void LogGraph()
        {
            for (int i = 0; i < DataGraph.Graph.Keys.Count; i++)
            {
                Debug.WriteLine(DataGraph.Graph.Keys.ElementAt(i));
                Debug.Write($"Parents ->");
                foreach (var i1 in DataGraph.Graph[DataGraph.Graph.Keys.ElementAt(i)])
                {
                   Debug.WriteLine($" {i1} \t");
                }
                Debug.Write("\n");
            }
        }
        /// <summary>
        /// Return Shortest Common Ancestor
        /// </summary>
        /// <param name="i1">id 1</param>
        /// <param name="i2">id 2</param>
        /// <param name="p">Storage of path length</param>
        /// <returns>Shortest Common Ancestor between two ids</returns>
        public int GetSca(int i1, int i2,out int p)
        {
            if(!_isInitialized)
                throw new DataException("Word Net is not initialized.");

            return _dataProcessor.GetSca(i1, i2,out p);
        }

        private int SemanticRelation(string s1, string s2)
        {
            HashSet<string> n;
            return GetSca(s1, s2, out n);
        }
        /// <summary>
        /// Get the shorteset common ancestor
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="scList"></param>
        /// <returns>The shortest length of path between s1 and s2</returns>
        public int GetSca(string s1, string s2,out HashSet<string> scList)
        {
            if (!_isInitialized)
                throw new DataException("Word Net is not initialized.");

            return _dataProcessor.GetSca(s1, s2,out scList);
        }
        public int GetSca(string s1, string s2, out HashSet<string> scList,Action<int,int> onQueryFinish)
        {
            if (!_isInitialized)
                throw new DataException("Word Net is not initialized.");

            return _dataProcessor.GetSca(s1, s2, out scList, onQueryFinish);
        }

        public int GetInputSize()
        {
            return DataGraph.NounsCount();
        }

        public string OutCastNoun(List<string> pNouns)
        {
            var max_Length=0; //= int.MaxValue;
            bool firstComparison = true;

            string outcast = "all equal";
            foreach (var pNoun in pNouns)
            {
                var summation = 0;
                foreach (var pNoun2 in pNouns)
                {
                    summation += SemanticRelation(pNoun, pNoun2);
                }

                if (firstComparison)
                {
                    max_Length = summation;
                    firstComparison = false;
                    outcast = pNoun;
                }
                else
                {
                    if (summation >= max_Length)
                    {
                        max_Length = summation;
                        outcast = pNoun;
                    }
                }
            }
            return outcast;
        }

        public string OutCastNoun(List<string> pNouns, Action<string, int, long> onNounFinish, string uniqueId)
        {
            var max_Length = 0; //= int.MaxValue;
            bool firstComparison = true;
            int ct = 0;
            string outcast = "all equal";
            var watch = Stopwatch.StartNew();
            foreach (var pNoun in pNouns)
            {
                ct++;
                var summation = 0;
                
                foreach (var pNoun2 in pNouns)
                {
                    summation += SemanticRelation(pNoun, pNoun2);
                }

                if (firstComparison)
                {
                    max_Length = summation;
                    firstComparison = false;
                    outcast = pNoun;
                }
                else
                {
                    if (summation >= max_Length)
                    {
                        max_Length = summation;
                        outcast = pNoun;
                    }
                }
                
            }
            onNounFinish(uniqueId, ct, watch.ElapsedMilliseconds);
            return outcast;
        }
    }
}