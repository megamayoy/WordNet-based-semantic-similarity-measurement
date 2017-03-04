using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordNetEngine
{
    // ReSharper disable once InconsistentNaming
    public class DAG
    {
        public Dictionary<int, HashSet<int>> Graph = new Dictionary<int, HashSet<int>>();
        public long DictionaryElapsedTime { get; set; }

        private readonly Dictionary<string, SortedSet<int>> _nounMap = new Dictionary<string, SortedSet<int>>();
        private readonly Dictionary<int, HashSet<string>> _synsetsMap = new Dictionary<int, HashSet<string>>();

        public DAG(string pSynsetDictionary,string pHypernyms)
        {
            ConstructGraph(pHypernyms);

            long elapsedTime;

            TryInitNouns(pSynsetDictionary, out elapsedTime);

            DictionaryElapsedTime = elapsedTime;

        }
        public DAG(string pSynsetDictionary, string pHypernyms,Action<int,int> progAction)
        {
            ConstructGraph(pHypernyms,progAction);

            long elapsedTime;

            TryInitNouns(pSynsetDictionary, out elapsedTime);

            DictionaryElapsedTime = elapsedTime;

        }

        private void ConstructGraph(string pHypernyms)
        {
            using (var reader = new StreamReader(pHypernyms))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    Debug.WriteLine($"Line Input {line}");
                    var v = line?.Split(',');
                    var child = v?[0];
                    var list = new HashSet<int>();
                    for (var i = 1; i < v.Count(); i++)
                    {
                        list.Add(int.Parse(v[i]));
                    }
                    var intChild = int.Parse(child);
                    if (Graph.ContainsKey(intChild))
                    {
                        Graph[intChild].UnionWith(list);
                        foreach (var i in Graph[intChild])
                        {
                            Debug.WriteLine($"SET UNION ---> {i}");
                        }
                        Debug.WriteLine("@@@@@@@@@@@@@@@@@@@");
                    }
                    else
                    {
                        Graph.Add(int.Parse(child), list);
                    }
                    //TODO report progress

                }
                Debug.WriteLine("Done");
            }
        }
        private void ConstructGraph(string pHypernyms,Action<int,int> progressReportAction)
        {
            using (var reader = new StreamReader(pHypernyms))
            {
                var input = reader.ReadToEnd();
                var lines = Regex.Split(input, "\r\n|\r|\n");
                for (int j = 0; j < lines.Length; j++)
                {
                    var line = lines[j];
                    Debug.WriteLine($"Line Input {line}");
                    var v = line?.Split(',');
                    var child = v?[0];
                    var list = new HashSet<int>();
                    for (var i = 1; i < v.Count(); i++)
                    {
                        list.Add(int.Parse(v[i]));
                    }
                    var intChild = int.Parse(child);
                    if (Graph.ContainsKey(intChild))
                    {
                        Graph[intChild].UnionWith(list);
                        foreach (var i in Graph[intChild])
                        {
                            Debug.WriteLine($"SET UNION ---> {i}");
                        }
                        Debug.WriteLine("@@@@@@@@@@@@@@@@@@@");
                    }
                    else
                    {
                        Graph.Add(int.Parse(child), list);
                    }
                    progressReportAction(j, lines.Length);
                }
                Debug.WriteLine("Done");
            }
        }

        #region Graph Functions

        /// <summary>
        /// Get all child vertices in graph.
        /// </summary>
        /// <returns>IEnumerable of integer vertices.</returns>
        public IEnumerable<int> Vertices()
        {
            return Graph.Keys;
        }

        private HashSet<int> GetParents(int child)
        {
            if(Graph.ContainsKey(child))
                return Graph[child];

            return new HashSet<int>();
        }
        public HashSet<int> this[int key] => GetParents(key);


        #endregion

        #region Mapping Functions
        /// <summary>
        /// Trye to initialize the Nouns to Ids Map throws exception if not.
        /// </summary>
        /// <param name="pInputPath">The file input path</param>
        /// <param name="elapsedTime">The total elapsed time of the process</param>
        /// <returns></returns>
        internal bool TryInitNouns(string pInputPath, out long elapsedTime)
        {
            try
            {
                InitNouns(pInputPath, out elapsedTime);
                return true;
            }
            catch
            {
                elapsedTime = -1;
                throw;
                // todo return false

            }
            finally
            {
                GC.Collect();
            }
        }
        private void InitNouns(string pFilePath, out long elapsed)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (var reader = new StreamReader(pFilePath))
            {
                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    var data = dataLine?.Split(','); // Split id, nouns , gloss
                    var nounId = -1;
                    int.TryParse(data?[0], out nounId); // Get Noun ID
                    var nouns = data?[1].Split(' '); //Get Nouns 
                    //Debug fail if nounId = -1
                    if (nounId == -1 || nouns == null)
                        throw new InvalidDataException("Invalid Data format");

                    //TODO it's better to combine both noun to id map and id to noun map here, for code redundancy
                    // sysnset map key = id, value = list of nouns of the same meaning
                    _synsetsMap.Add(nounId, new HashSet<string>(nouns));
                    foreach (var noun in nouns)
                    {
                        SortedSet<int> nounIds;
                        if (!_nounMap.TryGetValue(noun, out nounIds))
                        {
                            nounIds = new SortedSet<int>();
                        }
                        nounIds.Add(nounId);
                        if (!_nounMap.ContainsKey(noun))
                            _nounMap.Add(noun, nounIds);
                        else
                            _nounMap[noun] = nounIds;
                    }
                    //Finished adding nouns and mapping to ids
                }
            }
            //Clean Memory
            GC.Collect(1, GCCollectionMode.Optimized);
            watch.Stop();
            elapsed = watch.ElapsedMilliseconds;
        }
        internal bool ContainsNoun(string pNoun)
        {
            return _nounMap.ContainsKey(pNoun);
        }
        internal int NounsCount()
        {
            return _nounMap.Keys.Count;
        }
        internal IEnumerable<string> Nouns()
        {
            return _nounMap.Keys;
        }
        internal SortedSet<int> GetSynset(string noun)
        {
           return _nounMap[noun];
        }
        internal Dictionary<int, HashSet<string>>.ValueCollection Synsets()
        {
            return _synsetsMap.Values;
        }
        public HashSet<string> GetNounList(int pId)
        {
            return _synsetsMap[pId];
        }        
        #endregion
    }
}
