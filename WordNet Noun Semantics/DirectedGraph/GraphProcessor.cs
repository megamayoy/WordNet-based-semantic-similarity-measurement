using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WordNetEngine
{
    public class GraphProcessor
    {
        internal DAG Graph;

        public GraphProcessor(DAG dataDag,bool validation = true)
        {
            Graph = dataDag;
            if (validation)
            {
                if (!ValidateGraph(Graph))
                {
                    throw new InvalidDataException("Invalid Data Input, Graph is cycled or has more than one root.");
                }
            }
        }
        internal bool ValidateGraph(DAG pGraph)
        {
            var rooted = Rooted();
            var cycleFree = true;//!HasCycles(pGraph);
            return rooted && cycleFree;
        }
        internal bool Rooted()
        {
            var rootCount = 0;
            var root = 0;
            var rootAssigned = false;
            for (int i = 0; i < Graph.Vertices().Count(); i++)
            {
                var keyValue = Graph.Vertices().ElementAt(i);
                for (int j = 0; j < Graph[keyValue].Count; j++)
                {
                    var parentValue = Graph[keyValue].ElementAt(j);
                    if (Graph[parentValue].Count == 0)
                    {
                        Debug.WriteLine($"Root found here {parentValue}");
                        if (!rootAssigned)
                        {
                            root = parentValue;
                            rootCount++;
                            rootAssigned = true;
                        }
                        else
                        {
                            if (root != parentValue)
                            {
                                rootCount ++;
                                root = parentValue;
                            }
                        }

                    }
                }
            }
            Debug.WriteLine(rootCount != 1 ? "Not Rooted Graph" : $"Rooted Graph, Root = {root}");
            Debug.WriteLine("Done Graph Traverse");
            return rootCount == 1;
        }
        //TODO IMPLEMENT
        internal bool HasCycles(DAG pGraph)
        {
            throw new NotImplementedException();
        }
        internal int GetSca(int pNounId1, int pNounId2, out int pathLength )
        {
            return BiDirectionalBfs(pNounId1, pNounId2,out pathLength);
        }

        internal int GetSca(string pNoun1, string pNoun2, out HashSet<string> synsetsList)
        {
            var visitedStart = new Dictionary<int, bool>();
            var visitedGoal = new Dictionary<int, bool>();
            var distanceStart = new Dictionary<int, int>();
            var distanceGoal = new Dictionary<int, int>();
            var start = Graph.GetSynset(pNoun1);
            var goal = Graph.GetSynset(pNoun2);
            var minDistance = int.MaxValue;
            var sca = 0;
            var q = new Queue<KeyValuePair<int, char>>();
            //a char is whether an 's' or an 'g' to see where that node comes from in the current front of the queue
            for (var i = 0; i < start.Count; i++)
            {
                distanceStart.Add(start.ElementAt(i), 0);

                visitedStart.Add(start.ElementAt(i), true);
                q.Enqueue(new KeyValuePair<int, char>(start.ElementAt(i), 's'));
            }

            for (var i = 0; i < goal.Count; i++)
            {
                distanceGoal.Add(goal.ElementAt(i), 0);

                visitedGoal.Add(goal.ElementAt(i), true);
                q.Enqueue(new KeyValuePair<int, char>(goal.ElementAt(i), 'g'));
            }
            //traverse the graph !wisely
            while (q.Count > 0)
            {
                //check for an intersection
                //if it's a start node and  visited in goals nodes
                //if it's a goal node and  visited in start nodes
                //that's an intersection
                //perform a test
                var queueFront = q.Peek();
                if ((queueFront.Value == 's' && visitedGoal.ContainsKey(queueFront.Key)) ||
                    (queueFront.Value == 'g' && visitedStart.ContainsKey(queueFront.Key)))
                {
                    var currentDistance = distanceStart[queueFront.Key] + distanceGoal[queueFront.Key];

                    if (currentDistance < minDistance)
                    {
                        sca = q.Peek().Key;
                        minDistance = currentDistance;
                    }
                }
                //pushing parents section
                // if it's a goal parent and it's visited by another goal node,don't push it.
                //the same goes if a start parent is visited by another start nodes
                for (var i = 0; i < Graph[queueFront.Key].Count; i++)
                {
                    var node = Graph[queueFront.Key].ElementAt(i);

                    if (queueFront.Value == 's')
                    {
                        if (!visitedStart.ContainsKey(node))
                        {
                            visitedStart.Add(node, true);
                            q.Enqueue(new KeyValuePair<int, char>(node, 's'));

                            distanceStart.Add(node, distanceStart[queueFront.Key] + 1);
                        }
                    }
                    else if (queueFront.Value == 'g')
                    {
                        if (!visitedGoal.ContainsKey(node))
                        {
                            visitedGoal.Add(node, true);
                            q.Enqueue(new KeyValuePair<int, char>(node, 'g'));

                            distanceGoal.Add(node, distanceGoal[queueFront.Key] + 1);
                        }
                    }
                }


                q.Dequeue();
            }
            synsetsList = Graph.GetNounList(sca);
            return minDistance;
        }

        private int BiDirectionalBfs(int pNode1, int pNode2,out int pathLength)
        {
            var q1 = new Queue<int>();
            var q2 = new Queue<int>();

            var route1Visited = new Dictionary<int,bool>();
            var route2Visited = new Dictionary<int,bool>();

            var route1Distance = new Dictionary<int,int>();
            var route2Distance = new Dictionary<int,int>();

            var sca = -1;
            var minDistance = int.MaxValue;

            route1Distance.Add(pNode1, 0);
            route1Visited.Add(pNode1, true);
            q1.Enqueue(pNode1);

            while (q1.Count > 0)
            {
                var q1Peek = q1.Peek();
                
                for (var i = 0; i < Graph[q1Peek].Count; i++)
                {
                    if (!route1Visited.ContainsKey(Graph[q1Peek].ElementAt(i)))
                    {
                        route1Visited.Add(Graph[q1Peek].ElementAt(i), true);
                        if(route1Distance.ContainsKey(Graph[q1Peek].ElementAt(i)))
                            route1Distance[Graph[q1Peek].ElementAt(i)] = route1Distance[q1Peek] + 1;
                        else
                        {
                            route1Distance.Add(Graph[q1Peek].ElementAt(i), route1Distance[q1Peek] + 1);

                        }
                    }
                    q1.Enqueue(Graph[q1Peek].ElementAt(i));
                }
                q1.Dequeue();
            }

            q2.Enqueue(pNode2);
            route2Distance.Add(pNode2, 0);
            route2Visited.Add(pNode2, true);
            while (q2.Count > 0)
            {
                var q2Peek = q2.Peek();
                if (route1Visited.ContainsKey(q2Peek))
                {
                    var dist = route1Distance[q2Peek] + route2Distance[q2Peek];
                    if (dist < minDistance)
                    {
                        sca = q2Peek;
                        minDistance = dist;
                    }
                }

                for (int i = 0; i < Graph[q2Peek].Count; i++)
                {
                    if (!route2Visited.ContainsKey(Graph[q2Peek].ElementAt(i)))
                    {
                        route2Visited.Add(Graph[q2Peek].ElementAt(i), true);
                        if (route2Distance.ContainsKey(Graph[q2Peek].ElementAt(i)))
                            route2Distance[Graph[q2Peek].ElementAt(i)] = route2Distance[q2Peek] + 1;
                        else
                        {
                            route2Distance.Add(Graph[q2Peek].ElementAt(i), route2Distance[q2Peek] + 1);

                        }

                    }
                    q2.Enqueue(Graph[q2Peek].ElementAt(i));
                }
                q2.Dequeue();
            }

            pathLength = minDistance;
            return sca;
        }
        internal int GetSca(string pNoun1, string pNoun2, out HashSet<string> synsetsList,Action<int,int> onQueryFinish)
        {
            var n1Set = Graph.GetSynset(pNoun1);
            var n2Set = Graph.GetSynset(pNoun2);
            var dist = int.MaxValue;
            var minimumParentSca = 0;
            for (int i = 0; i < n1Set.Count; i++)
            {

                for (int j = 0; j < n2Set.Count; j++)
                {
                    int curDistance = -1;
                    var curParentSca = GetSca(n1Set.ElementAt(i), n2Set.ElementAt(j), out curDistance);
                    if (curDistance < dist)
                    {
                        dist = curDistance;
                        minimumParentSca = curParentSca;
                    }
                }

            }
            synsetsList = Graph.GetNounList(minimumParentSca);
            return dist;
        }

    }
}