// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************

// This class handles the graph traversal within the TraversalDemo scene
// and also performs in-editor tests

using UnityEngine;
using System.Collections.Generic; 
using System;
using MagnetRoads;

#if UNITY_EDITOR
using UnityEditor;
#endif 

/// <summary>
/// (TraversalDemo)
/// </summary>
namespace TraversalDemo
{
    /// <summary>s
    /// (TraversalDemo)
    /// </summary>
    public class RouteManager : MonoBehaviour
    {
        // Define master list of all TravelNodes in the map 
        /// <summary>
        /// List of all TravelNodes in the scene
        /// </summary>
        private List<TravelNode> _travelNodes;

        /// <summary>
        /// RouteManager's constructor method
        /// </summary>
        public RouteManager()
        {
            _travelNodes = new List<TravelNode>();
        }

        /// <summary>
        /// Populates the _travelNodes list with all existing TravelNodes
        /// </summary>
        public void FillNodeList()
        {
            ResetRouteList(); // clear existing graph
            TravelNode[] nodes = FindObjectsOfType<TravelNode>();
            foreach (TravelNode n in nodes) // fill the graph with all TravelNodes 
            {
                AddNodeToList(n);
            }
        }

        /// <summary>
        /// Clears the route list
        /// </summary>
        private void ResetRouteList()
        {
            _travelNodes.Clear();
        }

        /// <summary>
        /// Add a TravelNode to the list
        /// </summary>
        /// <param name="node"></param>
        private void AddNodeToList(TravelNode node)
        {
            _travelNodes.Add(node);
        }

        // Graph traversal
        /// <summary>
        /// Traversal method to find viable routes from one TravelNode to another
        /// </summary>
        /// <param name="from">From this TravelNode</param>
        /// <param name="to">To this TravelNode</param>
        /// <returns>An array of TravekNodes defining the route</returns>
        public TravelNode[] GetRouteFromTo(TravelNode from, TravelNode to)
        {
            Dictionary<TravelNode, bool> visited = new Dictionary<TravelNode, bool>();
            Dictionary<TravelNode, TravelNode> route = new Dictionary<TravelNode, TravelNode>();
            Queue<TravelNode> worklist = new Queue<TravelNode>();
            visited.Add(from, false);
            worklist.Enqueue(from);
            while (worklist.Count != 0) // traverse the graph
            {
                TravelNode node = worklist.Dequeue();
                foreach (TravelNode.Route r in node.connections)
                {
                    try
                    {
                        if ((!visited.ContainsKey(r.destination)) && (r.destination.CheckNodeActive()))
                        {
                            visited.Add(r.destination, false);
                            route.Add(r.destination, node);
                            worklist.Enqueue(r.destination);
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        Debug.LogError("Destination missing at: " + node.name + "\nPlease make sure all fields are full!");
                    }
                }
            }
            List<TravelNode> temp = new List<TravelNode>();
            try
            {
                while (route[to] != null)
                {
                    temp.Add(to);
                    to = route[to];
                }
            }
            catch (KeyNotFoundException) { } // do nothing - doesn't matter
            if (temp.Count < 1) return null; // nullify the array if not applicable
            temp.Add(from);
            temp.Reverse(); // reverse the list to get the correct ordering
            return temp.ToArray(); // convert to array
        }

        // Get a test route between two random nodes
        /// <summary>
        /// Returns a route from one random TravelNode to another
        /// </summary>
        /// <returns></returns>
        public Vector3[] GetRandomRoute()
        {
            int index1 = 0, index2 = 0;
            while (index1 == index2)
            {
                index1 = UnityEngine.Random.Range(0, _travelNodes.Count - 1);
                index2 = UnityEngine.Random.Range(0, _travelNodes.Count - 1);
            }
            return GetVectorsFromRoute(GetRouteFromTo(_travelNodes[index1], _travelNodes[index2]));
        }

        // Method to turn an existing route array into worldspace vectors
        /// <summary>
        /// Returns an array of vectors describing the route's path
        /// </summary>
        /// <param name="route">Valid array of TravelNodes</param>
        /// <returns>An array of Vector3's describing the route's path</returns>
        public Vector3[] GetVectorsFromRoute(TravelNode[] route)
        {
            if (route == null)
            {
                Debug.LogError("Null route! No vectors can be extrapolated.");
                return null;
            }
            List<Vector3> vectorList = new List<Vector3>();
            for (int i = 0; i < route.Length - 1; i++)
            {
                TravelNode start = route[i];
                TravelNode destination = route[i + 1];
                foreach (TravelNode.Route r in start.connections) // iterate through all the nodes in the route and extract worldspace vectors
                {
                    if (r.destination == destination)
                    {
                        vectorList.Add(r.startPoint.transform.position);
                        foreach (Vector3 v in r.GetRoute())
                        {
                            vectorList.Add(v); // add vectors that follow the road
                        }
                    }
                }
            }
            return vectorList.ToArray();
        }

        // Method to check a route is active
        /// <summary>
        /// Check a route for validity
        /// </summary>
        /// <param name="route">The array of TravelNodes to check</param>
        /// <returns>Boolean describing the validity of the route</returns>
        public bool CheckRouteIsActive(TravelNode[] route)
        {
            foreach (TravelNode node in route)
            {
                if (!node.CheckNodeActive()) return false;
            }
            return true;
        }
    }


	#if UNITY_EDITOR
    // This class handles the drawing of the RouteManager's custom inspector UI
    // Includes a brief guide on the RouteManager's function in the scene

    /// <summary>
    /// RouteManager's custom inspector UI
    /// </summary>
    [CustomEditor(typeof(RouteManager))]
    public class RouteTester : Editor
    {
        /// <summary>
        /// Reference to this RouteManager
        /// </summary>
        private RouteManager _manager;

        /// <summary>
        /// Debug destination
        /// </summary>
        private TravelNode _to;

        /// <summary>
        /// Debug start location
        /// </summary>
        private TravelNode _from;

        /// <summary>
        /// Fire once (flag)
        /// </summary>
        private bool _fireOnce = false;

        // Method to be fired once so as to not overload the node graph
        /// <summary>
        /// Initialization method to be invoked only upon the object's selection
        /// </summary>
        public void StartUpFireOnce()
        {
            if (!_fireOnce)
            {
                _manager.FillNodeList();
                _fireOnce = true;
            }
        }

        // Inspector interface
        /// <summary>
        /// Draw the custom inspector UI
        /// </summary>
        public override void OnInspectorGUI()
        {
            _manager = target as RouteManager;
            StartUpFireOnce();
            DrawDefaultInspector();
            GUILayout.Label("Route Tester", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This example adds a custom script onto the default intersections called 'Travel Nodes'\n\n" +
                "These nodes act as the nodes in a graph, connected by Spline Roads. This Route Manager class can make use of a traversal function to extrapolate a viable route from one of these nodes to the other." +
                "\n\nSelect two nodes from the 'Travel Nodes' section of the hierarchy and hit test to output a node route to the console. Alternatively, press the follower test button to see a cube navigate the route. The random test will spawn 5 cube followers and send them somewhere random.", MessageType.Info);
            EditorGUILayout.HelpBox("Regenerating intersection pieces will cause the 'StartPoint' connection information to be reset. This will break the test ability.", MessageType.Warning);
            var from = EditorGUILayout.ObjectField("From:", _from, typeof(TravelNode), true);
            _from = (TravelNode)from;
            var to = EditorGUILayout.ObjectField("To:", _to, typeof(TravelNode), true);
            _to = (TravelNode)to;
            if (GUILayout.Button("Test Route Finder"))
            {
                try
                {
                    var temp = _manager.GetRouteFromTo(_from, _to);
                    if (temp == null)
                    {
                        Debug.LogWarning("Invalid route from '" + _from.name + "' to '" + _to.name + "'"); // no valid route found
                    }
                    else
                    {
                        foreach (TravelNode t in _manager.GetRouteFromTo(_from, _to)) // print each destination in the route to the console
                        {
                            Debug.Log(t.name);
                        }
                    }
                }
                catch (Exception)
                {
                    Debug.LogWarning("No viable route found!");
                }
            }
            if (GUILayout.Button("Road Follower Test"))
            {
                try
                {
                    TravelNode[] temp = _manager.GetRouteFromTo(_from, _to);
                    DebugSpawnFollower(_manager.GetVectorsFromRoute(temp));
                }
                catch (Exception)
                {
                    Debug.LogWarning("Invalid road path or follower selected!");
                }
            }
            GUI.color = new Color(1, 0.5f, 0.0f);
            if (GUILayout.Button("Random Test"))
            {
                try
                {
                    DebugSpawnFollower(_manager.GetRandomRoute());
                    DebugSpawnFollower(_manager.GetRandomRoute());
                    DebugSpawnFollower(_manager.GetRandomRoute());
                    DebugSpawnFollower(_manager.GetRandomRoute());
                    DebugSpawnFollower(_manager.GetRandomRoute());
                }
                catch (Exception)
                {
                    Debug.LogError("GetRandomRoute() throwing error!");
                }
            }
        }

        // Spawn a follower object
        /// <summary>
        /// Spawn an in-editor follower that tracks a Vector3 array route
        /// </summary>
        /// <param name="roadPath">The vector route to follow</param>
        /// <returns>Returns the GameObject of the follower</returns>
        private GameObject DebugSpawnFollower(Vector3[] roadPath)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.transform.localScale = new Vector3(.2f, .2f, .2f);
            temp.AddComponent<RoadFollower>();
            temp.GetComponent<RoadFollower>().SetupRoadFollower(roadPath, 1);
            return temp;
        }
    }
	#endif
}