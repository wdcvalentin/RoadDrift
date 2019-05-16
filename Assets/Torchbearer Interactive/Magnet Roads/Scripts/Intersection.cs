// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************

// This class handles the mesh and information generation required to generate
// snappable intersections that link to SplineRoads

using UnityEngine;
using System;
using System.IO;
using TBUnityLib.MeshTools;
using RuntimeGizmo;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MagnetRoads v1.1.2
/// </summary>
namespace MagnetRoads
{
    /// <summary>
    /// MagnetRoads v1.1.2
    /// </summary>
    [ExecuteInEditMode] [AddComponentMenu("")]
    [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))] [RequireComponent(typeof(MeshCollider))]
    public class Intersection : MonoBehaviour
    {
        // Intersection Data Variables

        /// <summary>
        /// The Material to apply to the intersection surface
        /// </summary>
        [Tooltip("Road Material")]
        public Material surfaceMaterial;

        /// <summary>
        /// The Material to apply to the intersection's sides
        /// </summary>
        [Tooltip("Roadside Material")]
        public Material sideMaterial;

        /// <summary>
        /// The width of this Intersection
        /// </summary>
        [Tooltip("Road width value")]
        public float roadWidth;

        /// <summary>
        /// Depth of the road's sides
        /// </summary>
        [Tooltip("Depth of the road's sides")]
        public float sideDepth;

        /// <summary>
        /// Value defining the slope of the sides
        /// </summary>
        [Tooltip("Slope of the road's sides")]
        public float slopeWidth;

        /// <summary>
        /// Returns an array of all attached SnapPoints
        /// </summary>
        public SnapPoint[] SnapNodes { get { return gameObject.GetComponentsInChildren<SnapPoint>(); } }

        /// <summary>
        /// Returns an array of all attached StartPoints
        /// </summary>
        public StartPoint[] StartPoints { get { return gameObject.GetComponentsInChildren<StartPoint>(); } } 


        // Procedural Generation Variables

        /// <summary>
        /// The cached roadside Material
        /// </summary>
        private Material _cachedSideMaterial;

        /// <summary>
        /// The cached intersection position
        /// </summary>
        private Vector3 _cachedPosition;

        /// <summary>
        /// The cached rotation of this object
        /// </summary>
        private Quaternion _cachedRotation;

        /// <summary>
        /// The cached road width
        /// </summary>
        private float _cachedRoadWidth;

        /// <summary>
        /// The cached side depth
        /// </summary>
        private float _cachedSideDepth;

        /// <summary>
        /// The cached slope width
        /// </summary>
        private float _cachedSlopeWidth;

        /// <summary>
        /// Generated mesh
        /// </summary>
        private Mesh _mesh;

        /// <summary>
        /// Generated mesh's MeshFilter
        /// </summary>
        private MeshFilter _meshFilter;

        /// <summary>
        /// Generated mesh's MeshCollider
        /// </summary>
        private MeshCollider _meshCollider;

        /// <summary>
        /// Parent of the attached SnapPoints
        /// </summary>
        private GameObject _snapNodeParent;

        /// <summary>
        /// The type of intersection to generate (number of lanes)
        /// </summary>
        [SerializeField] [HideInInspector]
        private IntersectionType _intersectionType;

        /// <summary>
        /// Public accessor for the chosen intersection type
        /// </summary>
        public IntersectionType CurrentIntersectionType { get { return _intersectionType; } }


        // Runtime Editor Variables

        /// <summary>
        /// Reference to this Intersection's selectability script
        /// </summary>
        private GizmoSelectable _gizmoSelect;

        /// <summary>
        /// Is this Intersection runtime editable
        /// </summary>
        [SerializeField] [Tooltip("Set this Intersection to be editable at runtime")]
        private bool _isEditableAtRuntime;

        /// <summary>
        /// Returns whether this Intersection is editable at runtime
        /// </summary>
        public bool IsEditableAtRuntime { get { return _isEditableAtRuntime; } }
        
        
        // Enums

        /// <summary>
        /// Intersection lane number enum
        /// </summary>
        public enum IntersectionType
        {
            ThreeLane,
            FourLane
        }

        
        // Initialization Methods

        /// <summary>
        /// Intersection initialization method
        /// </summary>
        /// <param name="type">Intersection type</param>
        public void SetUp(IntersectionType type)
        {
            _intersectionType = type;
            roadWidth = 0.5f;
            sideDepth = 0.2f;
            _cachedPosition = transform.position;
            _cachedRotation = transform.rotation;
            _cachedRoadWidth = roadWidth;
            _cachedSideDepth = sideDepth;
            _cachedSlopeWidth = slopeWidth;
            GenerateIntersectionMesh();
        }

        
        // Update Methods

        /// <summary>
        /// Draw call for in-editor graphics
        /// </summary>
        public void Update()
        {
            // Store a local instance of the gizmo if possible
            Gizmo gizmo = FindObjectOfType<Gizmo>();

            // Check whether to update the intersection mesh
            if (_isEditableAtRuntime)
            {
                if (transform.position != _cachedPosition || transform.rotation != _cachedRotation || roadWidth != _cachedRoadWidth || sideDepth != _cachedSideDepth || slopeWidth != _cachedSlopeWidth)
                {
                    GenerateIntersectionMesh();
                }
            }

            // Check if this intersection needs to be made selectable or remove the selectable gizmo
            if (_isEditableAtRuntime && Application.isPlaying)
            {
                if (GetComponent<GizmoSelectable>()) _gizmoSelect = GetComponent<GizmoSelectable>();
                else _gizmoSelect = gameObject.AddComponent<GizmoSelectable>();
            }
            else
            {
                // Check for the existing gizmo and clear its data
                if (gizmo)
                {
                    foreach (Transform selectedObj in gizmo.SelectedObjects)
                    {
                        if (selectedObj.gameObject == gameObject)
                        {
                            gizmo.ClearSelection();
                            gizmo.Hide();
                            break;
                        }
                    }
                }
                DestroyImmediate(_gizmoSelect);
            }

            // Constrain values
            if (roadWidth < 0) roadWidth = 0.01f; 
            if (slopeWidth < 0) slopeWidth = 0;

            // Cache required variables
            _cachedPosition = transform.position;
            _cachedRotation = transform.rotation;
            _cachedRoadWidth = roadWidth;
            _cachedSideDepth = sideDepth;
            _cachedSlopeWidth = slopeWidth;

            // When selected by the editor gizmo hide the snapPoints
            if (gizmo)
            {
                foreach (SnapPoint point in SnapNodes)
                {
                    if (point.GetComponent<Renderer>()) point.GetComponent<Renderer>().enabled = gizmo.SelectedObjects.Contains(transform) ? false : true;
                }
            }
        }

        
        // Intersection Generation Methods

        /// <summary>
        /// Generate this Intersection's mesh and assign it
        /// </summary>
        public void GenerateIntersectionMesh()
        {
            // Store roadSide texture
            _cachedRotation = transform.rotation;
            if (transform.Find("Intersection Sides"))
                _cachedSideMaterial = transform.Find("Intersection Sides").gameObject.GetComponent<Renderer>().sharedMaterial;

            // Refresh object information
            foreach (SnapPoint node in SnapNodes)
            {
                DestroyImmediate(node.gameObject);
            }
            foreach (StartPoint point in StartPoints)
            {
                DestroyImmediate(point.gameObject);
            }
            if (_snapNodeParent) DestroyImmediate(_snapNodeParent);
            if (transform.Find("Intersection Underside")) DestroyImmediate(transform.Find("Intersection Underside").gameObject);
            if (transform.Find("Intersection Sides")) DestroyImmediate(transform.Find("Intersection Sides").gameObject);
            transform.rotation = Quaternion.Euler(0, 0, 0); // reset any rotation

            // Set-up mesh components
            try
            {
                _meshFilter = GetComponent<MeshFilter>();
            }
            catch (NullReferenceException)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            try
            {
                GetComponent<MeshRenderer>();
            }
            catch (NullReferenceException)
            {
                gameObject.AddComponent<MeshRenderer>();
            }
            try
            {
                _meshCollider = GetComponent<MeshCollider>();
            }
            catch (NullReferenceException)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            // Create the SnapPoint parent object
            _snapNodeParent = new GameObject();
            _snapNodeParent.transform.position = transform.position;
            _snapNodeParent.transform.parent = transform;
            _snapNodeParent.hideFlags = HideFlags.HideInHierarchy;
            _snapNodeParent.name = "Snap Points";

            // Generate road mesh
            _mesh = Geometry.GeneratePlaneMesh(roadWidth, roadWidth);
            _mesh.name = "Procedural Intersection";
            _meshFilter.mesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            if (surfaceMaterial) GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            // Generate side mesh & game object
            GameObject sides = new GameObject();
            Mesh sideMesh = Geometry.GenerateTetrahedron(roadWidth, roadWidth, sideDepth, roadWidth+slopeWidth, roadWidth+slopeWidth, false, false);
            sides.AddComponent<MeshFilter>().mesh = sideMesh;
            sides.AddComponent<MeshRenderer>();
            sides.AddComponent<MeshCollider>().sharedMesh = sideMesh;
            if (!sideMaterial) sides.GetComponent<Renderer>().sharedMaterial = _cachedSideMaterial;
            else sides.GetComponent<Renderer>().sharedMaterial = sideMaterial;
            sides.transform.position = transform.position;
            sides.transform.SetParent(transform);
            //sides.gameObject.hideFlags = HideFlags.HideInHierarchy;
            sides.name = "Intersection Sides";

            // Generate underside mesh
            GameObject underSide = new GameObject();
            Mesh underSideMesh = Geometry.GeneratePlaneMesh(roadWidth, roadWidth);
            underSide.AddComponent<MeshFilter>().mesh = underSideMesh;
            underSide.AddComponent<MeshRenderer>();
            underSide.AddComponent<MeshCollider>().sharedMesh = underSideMesh;
            if (!sideMaterial) underSide.GetComponent<Renderer>().sharedMaterial = _cachedSideMaterial;
            else underSide.GetComponent<Renderer>().sharedMaterial = sideMaterial;
            underSide.transform.position = new Vector3(transform.position.x, transform.position.y - sideDepth, transform.position.z);
            underSide.transform.Rotate(new Vector3(180, 0, 0));
            underSide.transform.SetParent(transform);
            underSide.gameObject.hideFlags = HideFlags.HideInHierarchy;
            underSide.name = "Intersection Underside";

            // Generate snap points on each edge of the intersection
            if (_intersectionType == IntersectionType.ThreeLane)
            {
                CreateSnapPoint(Vector3.left * (roadWidth / 2), Quaternion.Euler(0, -90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint1");
                CreateSnapPoint(Vector3.forward * (roadWidth / 2), Quaternion.Euler(0, 0, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint2");
                CreateSnapPoint(Vector3.right * (roadWidth / 2), Quaternion.Euler(0, 90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint3");
                CreateStartPoint((Vector3.left * (roadWidth / 4)) + (Vector3.back * (roadWidth / 4)), roadWidth / 8, "StartPoint1");
                CreateStartPoint((Vector3.left * (roadWidth / 4)) + (Vector3.forward * (roadWidth / 4)), roadWidth / 8, "StartPoint2");
                CreateStartPoint((Vector3.right * (roadWidth / 4)) + (Vector3.forward * (roadWidth / 4)), roadWidth / 8, "StartPoint3");
                CreateStartPoint((Vector3.right * (roadWidth / 4)) + (Vector3.back * (roadWidth / 4)), roadWidth / 8, "StartPoint4");
            }
            if (_intersectionType == IntersectionType.FourLane)
            {
                CreateSnapPoint(Vector3.left * (roadWidth / 2), Quaternion.Euler(0, -90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint1");
                CreateSnapPoint(Vector3.forward * (roadWidth / 2), Quaternion.Euler(0, 0, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint2");
                CreateSnapPoint(Vector3.right * (roadWidth / 2), Quaternion.Euler(0, 90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint3");
                CreateSnapPoint(Vector3.back * (roadWidth / 2), Quaternion.Euler(0, 180, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint4");
                CreateStartPoint((Vector3.left * (roadWidth / 4)) + (Vector3.back * (roadWidth / 4)), roadWidth / 8, "StartPoint1");
                CreateStartPoint((Vector3.left * (roadWidth / 4)) + (Vector3.forward * (roadWidth / 4)), roadWidth / 8, "StartPoint2");
                CreateStartPoint((Vector3.right * (roadWidth / 4)) + (Vector3.forward * (roadWidth / 4)), roadWidth / 8, "StartPoint3");
                CreateStartPoint((Vector3.right * (roadWidth / 4)) + (Vector3.back * (roadWidth / 4)), roadWidth / 8, "StartPoint4");
            }

            // Rotate back into place
            transform.rotation = _cachedRotation;
        }

        /// <summary>
        /// Generate this intersection's SnapPoints
        /// </summary>
        /// <param name="offset">Offset from center</param>
        /// <param name="rotation">SnapPoint rotation</param>
        /// <param name="polarity">Magnet polarity (bipolar)</param>
        /// <param name="name">SnapPoint name</param>
        /// <returns>The attached SnapPoint's GameObject</returns>
        private GameObject CreateSnapPoint(Vector3 offset, Quaternion rotation, SnapPoint.PointEnd polarity, string name)
        {
            GameObject snapPoint = new GameObject();
            snapPoint.AddComponent<SnapPoint>().SetUp(polarity, roadWidth);
            snapPoint.transform.position = transform.position + offset;
            snapPoint.transform.rotation = rotation;
            snapPoint.transform.parent = _snapNodeParent.transform;
            snapPoint.name = name;
            return snapPoint;
        }

        /// <summary>
        /// Generate this Intersection's StartPoints
        /// </summary>
        /// <param name="offset">Offset from center</param>
        /// <param name="radius">StartPoint editor sphere radius</param>
        /// <param name="name">StartPoint name</param>
        /// <returns>The attached StartPoint's GameObject</returns>
        private GameObject CreateStartPoint(Vector3 offset, float radius, string name)
        {
            GameObject startPoint = new GameObject();
            startPoint.AddComponent<StartPoint>();
            startPoint.transform.position = transform.position + offset;
            startPoint.transform.parent = transform;
            startPoint.name = name;
            return startPoint;
        }


        // Runtime Editor Methods
        
        /// <summary>
        /// Enables the editing of the Intersection at runtime
        /// </summary>
        public void EnableRuntimeEditing()
        {
            _isEditableAtRuntime = true;
        }

        /// <summary>
        /// Disables the editing of the intersection at runtime
        /// </summary>
        public void DisableRuntimeEditing()
        {
            _isEditableAtRuntime = false;
        }


        // Intersection Saving

        /// <summary>
        /// Saves this intersection to a single .xml file
        /// </summary>
        /// <param name="path"></param>
        public void SaveIntersectionToXML(string path = "DEFAULT_LOCATION")
        {
            try
            {
                MagnetRoadCollection collection = new MagnetRoadCollection();
                Intersection[] intersection = new Intersection[1];
                intersection[0] = this;
                collection.PrepareIntersectionData(intersection);
                if (path == "DEFAULT_LOCATION") collection.Save(Path.Combine(Application.persistentDataPath, "RoadData.xml"));
                else collection.Save(path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Intersection to a file, check the selected path.");
            }
        }
    }


    // This class handles the editable attributes of the intersection
    // exposing them to the inspector 

    /// <summary>
    /// Intersection's custom inspector UI
    /// </summary>
	#if UNITY_EDITOR
    [CustomEditor(typeof(Intersection))]
    public class IntersectionEditorInspector : Editor
    {
        /// <summary>
        /// Reference to this Intersection
        /// </summary>
        private Intersection _intersection;

        /// <summary>
        /// Reference to the Torchbearer Logo
        /// </summary>
        private Texture _tbLogo;

        /// <summary>
        /// OnEnable load the inspector logo
        /// </summary>
        private void OnEnable()
        {
            _tbLogo = (Texture)Resources.Load("TBLogo", typeof(Texture));
        }

        /// <summary>
        /// Render the Intersection's custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            _intersection = target as Intersection;
            GUILayout.Label(_tbLogo, GUILayout.Width(EditorGUIUtility.currentViewWidth - 40.0f), GUILayout.Height(60.0f));

            // Intersection editing
            GUILayout.Label("Intersection Data:", EditorStyles.boldLabel);
            DrawDefaultInspector();
            var oldColor = GUI.color;
            GUI.color = new Color(1, 0.5f, 0.0f);

            // Intersection generation
            GUILayout.Label("Generation:", EditorStyles.boldLabel);
            if (_intersection.IsEditableAtRuntime) GUI.enabled = false;
            if (GUILayout.Button("Regenerate Intersection Mesh"))
            {
                Undo.RecordObject(_intersection, "Generate Intersection Mesh");
                EditorUtility.SetDirty(_intersection);
                _intersection.GenerateIntersectionMesh();
            }
            if (_intersection.IsEditableAtRuntime) GUI.enabled = true;
            GUI.color = oldColor;

            // Save road
            GUILayout.Label("Save:", EditorStyles.boldLabel);
            GUI.color = new Color(.2f, .55f, 1);
            if (GUILayout.Button("Save Selected Intersection to XML"))
            {
                string path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledIntersection", "xml");
                try
                {
                    _intersection.SaveIntersectionToXML(path);
                }
                catch (ArgumentException)
                {
                    // No folder selected - ignore
                }
            }
        }
    }
	#endif
}