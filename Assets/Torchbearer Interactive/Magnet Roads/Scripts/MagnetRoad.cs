// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by:
// Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// Edward S Andrew    - ed@tbinteractive.co.uk, 2016
// ************************************************************************

// This class handles the specic mesh generation and information neccecary to 
// create and retrieve information from spline roads as well as perform saving
// and loading functions.

// Magnet Roads v1.1.2 Changelog
// + Updated project to Unity 5.6
// + Moved dropdown menu to Tools
// + Updated to latest TBUnityLib

using UnityEngine;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using TBUnityLib.Generic;
using TBUnityLib.MeshTools;
using RuntimeGizmo;
using BezierSplines;

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
    [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))] [RequireComponent(typeof(BezierSpline))] [RequireComponent(typeof(MeshCollider))]
    public class MagnetRoad : MonoBehaviour
    {
        // Road Data Variables
        
        /// <summary>
        /// This road's BezierSpline source
        /// </summary>
        [HideInInspector]
        public BezierSpline splineSource;

        /// <summary>
        /// The Material to apply to the road surface
        /// </summary>
        [Tooltip("Road Material")]
        public Material surfaceMaterial;

        /// <summary>
        /// The Material to apply to the road's sides
        /// </summary>
        [Tooltip("Roadside Material")]
        public Material sideMaterial;

        /// <summary>
        /// The width of this MagnetRoad
        /// </summary>
        [Tooltip("Road width value")]
        public float roadWidth;

        /// <summary>
        /// Depth of the road's sides
        /// </summary>
        [Tooltip("Depth of the road's sides")]
        public float sideDepth;
        
        /// <summary>
        /// The distance from the bottom of the side ramp to the road side
        /// </summary>
        [Tooltip("The distance from the bottom of the side ramp to the road side")]
        public float slopeWidth;

        /// <summary>
        /// The number of steps in the curve (mesh resolution)
        /// </summary>
        [Tooltip("Steps per curve")]
        public int stepsPerCurve;

        /// <summary>
        /// Show road outline (flag)
        /// </summary>
        [Tooltip("Show road outline")]
        public bool showRoadOutline;

        /// <summary>
        /// Show car routes (flag)
        /// </summary>
        [Tooltip("Show car routes")]
        public bool showCarRoutes;

        /// <summary>
        /// Use custom car (flag)
        /// </summary>
        [HideInInspector]
        public bool customCar;

        /// <summary>
        /// Custom car GameObject source
        /// </summary>
        [HideInInspector]
        public GameObject carObject;

        /// <summary>
        /// Reference to the left snap node object
        /// </summary>
        private GameObject _snapNodeLeft;

        /// <summary>
        /// Reference to the right snap node object
        /// </summary>
        private GameObject _snapNodeRight;

        /// <summary>
        /// Returns the left SnapPoints's Transform
        /// </summary>
        public Transform SnapNodeLeft { get { return transform.Find("SnapNodeLeft"); } }

        /// <summary>
        /// Returns the right SnapPoint's Transform
        /// </summary>
        public Transform SnapNodeRight { get { return transform.Find("SnapNodeRight"); } }


        // Procedural Generation Variables
        
        /// <summary>
        /// Left road side GameObject (child)
        /// </summary>
        [HideInInspector]
        private GameObject _leftSide;

        /// <summary>
        /// Right road side GameObject (child)
        /// </summary>
        [HideInInspector]
        private GameObject _rightSide;

        /// <summary>
        /// Front road side GameObject (child)
        /// </summary>
        [HideInInspector]
        private GameObject _frontSide;

        /// <summary>
        /// Back road side GameObject (child)
        /// </summary>
        [HideInInspector]
        private GameObject _backSide;

        /// <summary>
        /// Road underside GameObject (child)
        /// </summary>
        [HideInInspector]
        private GameObject _underSide;

        /// <summary>
        /// Cached roadside Material
        /// </summary>
        private Material _cachedSideMaterial;

        /// <summary>
        /// Generated Mesh
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


        // Runtime Editor Variables

        /// <summary>
        /// Reference to the Gizmo in the scene (if needed)
        /// </summary>
        private Gizmo _gizmo;

        /// <summary>
        /// Reference to this magnet road's Gizmo Select 
        /// </summary>
        private GizmoSelectable _gizmoSelect;

        /// <summary>
        /// Used to draw the bezier curve in play mode
        /// </summary>
        private LineRenderer _runtimeCurveLine;

        /// <summary>
        /// Used to draw the bezier handles' directions
        /// </summary>
        private LineRenderer[] _runtimeHandleLines;

        /// <summary>
        /// Array of runtime curve handles
        /// </summary>
        private GameObject[] _runtimeHandles;

        /// <summary>
        /// The cached locations of the spline's curve handles
        /// </summary>
        private Vector3[] _cachedPointVectors;

        /// <summary>
        /// Is this Magnet Road runtime editable
        /// </summary>
        [SerializeField] [Tooltip("Set this Magnet Road to be editable at runtime")]
        private bool _isEditableAtRuntime;

        /// <summary>
        /// Returns whether this Magnet Road is editable at runtime
        /// </summary>
        public bool IsEditableAtRuntime { get { return _isEditableAtRuntime; } }


        // Enums

        /// <summary>
        /// Transform offset types
        /// </summary>
        private enum OffsetTransform
        {
            None,
            Positive,
            Negative
        }


        // Initialization Methods

        /// <summary>
        /// MagnetRoad's constructor method
        /// </summary>
        public MagnetRoad()
        {
            showRoadOutline = true;
            showCarRoutes = true;
            sideDepth = 0.2f;
            slopeWidth = 0.0f;
            roadWidth = 0.5f;
            stepsPerCurve = 20;
            customCar = false;
            carObject = null;
        }

        /// <summary>
        /// On awake make sure the spline source exists and initialize the Magnet Road
        /// </summary>
        private void Awake()
        {
            // Check spline exists
            if (splineSource == null)
            {
                // Create spline or throw error
                try
                {
                    splineSource = GetComponent<BezierSpline>();
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning("Spline Road missing Bezier Spline! Component added automatically.");
                    splineSource = gameObject.AddComponent<BezierSpline>();
                }
                
                // Perform some inspector formatting
                #if UNITY_EDITOR
                for (int i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(this);
                for (int i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(splineSource);
                #endif
            }

            // initialize the runtime handles & gizmo (if applicable)
            _runtimeHandles = new GameObject[0];
            _runtimeHandleLines = new LineRenderer[0];
            _cachedPointVectors = new Vector3[0];
        }

        /// <summary>
        /// On start cleanup any lingering handles and replace them 
        /// </summary>
        private void Start()
        {
            CleanupRuntimeHandles();
            InitializeRuntimeHandles();
        }


        // Update Methods

        /// <summary>
        /// Draw call for in-editor graphics
        /// </summary>
        private void OnDrawGizmos()
        {
			#if UNITY_EDITOR
            Tools.hidden = false;
            if (!Application.isPlaying || !_isEditableAtRuntime)
            {
                CleanupRuntimeHandles();  // tidy up runtime editor stuff when in unity editor
                if (_gizmo) DestroyImmediate(_gizmo);
            }
			#endif

            if (roadWidth <= 0) roadWidth = 0.01f; // constrain lower bound
            if (stepsPerCurve <= 0) stepsPerCurve = 1; // constrain lower bound
            if (showCarRoutes) DrawCarPath(GenerateCarPathVectors( roadWidth));
            if (showRoadOutline)
            {
                DrawRoadOutline(GenerateRoadVertexOutput(roadWidth));
                DrawRoadOutline(GenerateLeftRoadSideVectors(GenerateRoadVertexOutput(roadWidth)));
                DrawRoadOutline(GenerateRightRoadSideVectors(GenerateRoadVertexOutput(roadWidth)));
            }
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);
        }

        /// <summary>
        /// Magnet Road's per frame update loop - handles gizmo selectability for runtime editor
        /// </summary>
        private void Update()
        {
            // Handle the realtime editing functionality
            if (_isEditableAtRuntime && Application.isPlaying)
            {
                // Get a reference to the gizmo when possible
                if (!_gizmo)
                {
                    if (FindObjectOfType<Gizmo>()) _gizmo = FindObjectOfType<Gizmo>();
                }
                // Generate & display curve lines and make the road selectable
                if (_runtimeCurveLine && !_runtimeCurveLine.enabled) _runtimeCurveLine.enabled = true;
                if (GetComponent<GizmoSelectable>()) _gizmoSelect = GetComponent<GizmoSelectable>();
                if (!_gizmoSelect || !_gizmoSelect.GetComponent<GizmoSelectable>()) _gizmoSelect = gameObject.AddComponent<GizmoSelectable>();
                if (_runtimeHandles.Length < 1)
                {
                    CleanupRuntimeHandles();
                    InitializeRuntimeHandles();
                }
            }
            else
            {
                CleanupRuntimeHandles();  // tidy up runtime editor stuff when in unity editor
                DestroyImmediate(_gizmoSelect);
            }

            // Update realtime editor handles
            UpdateRuntimeHandles();

            // Update cached point vectors for file saving
            _cachedPointVectors = new Vector3[splineSource.ControlPointCount];
            for (int i = 0; i < splineSource.ControlPointCount; i++)
            {
                _cachedPointVectors[i] = splineSource.GetControlPoint(i);
            }
        }


        // Road Generation Methods

        /// <summary>
        /// Generate road mesh data from BezierSpline
        /// </summary>
        /// <param name="spline">BezierSpline source</param>
        /// <param name="stepsPerCurve">Steps per curve (mesh resolution)</param>
        /// <param name="roadWidth">Road mesh width</param>
        /// <returns>An array of vector pairs describing the MagnetRoad's surface</returns>
        public Pair<Vector3>[] GenerateRoadVertexOutput(float roadWidth)
        {
            Pair<Vector3>[] vertexOutput = new Pair<Vector3>[(stepsPerCurve * splineSource.CurveCount) + 1];
            int index = 0;
            float roadOffset = roadWidth / 2;
            Vector3 point = splineSource.GetPoint(0f);
            Pair<Vector3> current = new Pair<Vector3>();
            Quaternion offsetRotation = Quaternion.Euler(0, 90, 0); // 90 degrees
            var vaTemp = point + (splineSource.GetOffsetRotation(0f, offsetRotation) * roadOffset);
            current.First = new Vector3(vaTemp.x, point.y, vaTemp.z);
            var vbTemp = point + (splineSource.GetOffsetRotation(0f, offsetRotation) * -roadOffset);
            current.Second = new Vector3(vbTemp.x, point.y, vbTemp.z);
            vertexOutput[index] = current;
            int steps = stepsPerCurve * splineSource.CurveCount;
            for (int i = 0; i <= steps; i++, index++)
            {
                point = splineSource.GetPoint(i / (float)steps);
                vaTemp = point + (splineSource.GetOffsetRotation(i / (float)steps, offsetRotation) * roadOffset);
                current.First = new Vector3(vaTemp.x, point.y, vaTemp.z);
                vbTemp = point + (splineSource.GetOffsetRotation(i / (float)steps, offsetRotation) * -roadOffset);
                current.Second = new Vector3(vbTemp.x, point.y, vbTemp.z);
                vertexOutput[index] = current;
            }
            return vertexOutput;
        }

        /// <summary>
        /// Generate a vector array of car paths (1/2 road width)
        /// </summary>
        /// <param name="spline">BezierSpline source</param>
        /// <param name="stepsPerCurve">Steps per curve (mesh resolution)</param>
        /// <param name="roadWidth">Road mesh width</param>
        /// <returns>An array of vector pairs describing the road's lanes</returns>
        public Pair<Vector3>[] GenerateCarPathVectors(float roadWidth)
        {
            return GenerateRoadVertexOutput(roadWidth / 2);
        }

        /// <summary>
        /// Output data for the left roadside mesh
        /// </summary>
        /// <param name="vertexData">Road vertex data</param>
        /// <returns>An array of vector pairs describing the right roadside</returns>
        public Pair<Vector3>[] GenerateLeftRoadSideVectors(Pair<Vector3>[] vertexData)
        {
            Pair<Vector3>[] leftRoadSide = vertexData;
            Pair<Vector3>[] leftRoadSideSlope = GenerateRoadVertexOutput(roadWidth + slopeWidth);
            for (int i = 0; i < leftRoadSide.Length; i++)
            {
                leftRoadSide[i].Second = new Vector3(leftRoadSideSlope[i].First.x, leftRoadSideSlope[i].First.y - sideDepth, leftRoadSideSlope[i].First.z);
            }
            return leftRoadSide;
        }

        /// <summary>
        /// Output data for the right roadside mesh
        /// </summary>
        /// <param name="vertexData">Road vertex data</param>
        /// <returns>An array of vector pairs describing the right roadside</returns>
        public Pair<Vector3>[] GenerateRightRoadSideVectors(Pair<Vector3>[] vertexData)
        {
            Pair<Vector3>[] rightRoadSide = vertexData;
            Pair<Vector3>[] rightRoadSideSlope = GenerateRoadVertexOutput(roadWidth + slopeWidth);
            for (int i = 0; i < rightRoadSide.Length; i++)
            {
                rightRoadSide[i].First = new Vector3(rightRoadSideSlope[i].Second.x, rightRoadSideSlope[i].Second.y - sideDepth, rightRoadSideSlope[i].Second.z);
            }
            return rightRoadSide;
        }

        /// <summary>
        /// Generate the road mesh and add it to this MagnetRoad
        /// </summary>
        /// <param name="vertexData">Road vertex data</param>
        public void GenerateRoadMesh(Pair<Vector3>[] vertexData)
        {
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

            // Generate road mesh
            _mesh = new Mesh();
            _meshFilter.mesh = Geometry.GenerateStrip(vertexData, transform, true, false, "ProceduralRoad");
            _meshCollider.sharedMesh = _meshFilter.sharedMesh;
            if (surfaceMaterial) gameObject.GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            // Generate helper points 
            if (!SnapNodeLeft || !SnapNodeRight) GenerateSnapPoints(splineSource, stepsPerCurve);
            UpdateSnapPoints();

            // Generate the side meshes
            GenerateSideMeshes(
                GenerateLeftRoadSideVectors(GenerateRoadVertexOutput(roadWidth)),
                GenerateRightRoadSideVectors(GenerateRoadVertexOutput(roadWidth)));
        }

        /// <summary>
        /// Generate the road side meshes and add them to this MagnetRoad
        /// </summary>
        /// <param name="leftSideVectors">Left roadside mesh data</param>
        /// <param name="rightSideVectors">Right roadside mesh data</param>
        public void GenerateSideMeshes(Pair<Vector3>[] leftSideVectors, Pair<Vector3>[] rightSideVectors)
        {
            // Clear existing child mesh game objects from the road and generate new ones
            if (transform.Find("Road Side One"))
            {
                _cachedSideMaterial = transform.Find("Road Side One").gameObject.GetComponent<Renderer>().sharedMaterial; // store the material for later use
                DestroyImmediate(transform.Find("Road Side One").gameObject);
            }
            if (transform.Find("Road Side Two")) DestroyImmediate(transform.Find("Road Side Two").gameObject);
            if (transform.Find("Road Underside")) DestroyImmediate(transform.Find("Road Underside").gameObject);
            if (transform.Find("Road Side Three")) DestroyImmediate(transform.Find("Road Side Three").gameObject);
            if (transform.Find("Road Side Four")) DestroyImmediate(transform.Find("Road Side Four").gameObject);
            _rightSide = new GameObject("Road Side One");
            _rightSide.transform.parent = gameObject.transform;
            _rightSide.hideFlags = HideFlags.HideInHierarchy;
            _leftSide = new GameObject("Road Side Two");
            _leftSide.transform.parent = gameObject.transform;
            _leftSide.hideFlags = HideFlags.HideInHierarchy;
            _frontSide = new GameObject("Road Side Three");
            _frontSide.transform.parent = gameObject.transform;
            _frontSide.hideFlags = HideFlags.HideInHierarchy;
            _backSide = new GameObject("Road Side Four");
            _backSide.transform.parent = gameObject.transform;
            _backSide.hideFlags = HideFlags.HideInHierarchy;
            _underSide = new GameObject("Road Underside");
            _underSide.transform.parent = gameObject.transform;
            _underSide.hideFlags = HideFlags.HideInHierarchy;

            // Create RoadSideOne's mesh and mesh filter components
            MeshFilter rsOneMf = _rightSide.AddComponent<MeshFilter>();
            _rightSide.AddComponent<MeshRenderer>();
            rsOneMf.mesh = Geometry.GenerateStrip(rightSideVectors, transform, false, null, "RoadSideOne");
            _rightSide.AddComponent<MeshCollider>().sharedMesh = rsOneMf.sharedMesh;
            if (!sideMaterial) _rightSide.GetComponent<Renderer>().sharedMaterial = _cachedSideMaterial;
            else _rightSide.GetComponent<Renderer>().sharedMaterial = sideMaterial;

            // Do the same for RoadSideTwo
            MeshFilter rsTwoMf = _leftSide.AddComponent<MeshFilter>();
            _leftSide.AddComponent<MeshRenderer>();
            rsTwoMf.mesh = Geometry.GenerateStrip(leftSideVectors, transform, false, null, "RoadSideTwo");
            _leftSide.AddComponent<MeshCollider>().sharedMesh = rsTwoMf.sharedMesh;
            if (!sideMaterial) _leftSide.GetComponent<Renderer>().sharedMaterial = _cachedSideMaterial;
            else _leftSide.GetComponent<Renderer>().sharedMaterial = sideMaterial;

            // Generate the mesh for the front cap of the road
            MeshFilter rsFrontMf = _frontSide.AddComponent<MeshFilter>();
            _frontSide.AddComponent<MeshRenderer>();
            rsFrontMf.mesh = Geometry.GeneratePlaneMesh(leftSideVectors[0].First, rightSideVectors[0].Second, leftSideVectors[0].Second, rightSideVectors[0].First, true);
            if (sideDepth > 0) _frontSide.AddComponent<MeshCollider>().sharedMesh = rsFrontMf.sharedMesh; // errors & pain abound if the sideDepth is =< 0 and you try and add a mesh collider
            rsFrontMf.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? _cachedSideMaterial : sideMaterial;

            // Generate the mesh for the back cap of the road
            MeshFilter rsBackMf = _backSide.AddComponent<MeshFilter>();
            _backSide.AddComponent<MeshRenderer>();
            rsBackMf.mesh = Geometry.GeneratePlaneMesh(leftSideVectors[leftSideVectors.Length - 1].First, rightSideVectors[rightSideVectors.Length - 1].Second, leftSideVectors[leftSideVectors.Length - 1].Second, rightSideVectors[rightSideVectors.Length - 1].First, false);
            if (sideDepth > 0) _backSide.AddComponent<MeshCollider>().sharedMesh = rsBackMf.sharedMesh; // errors & pain abound if the sideDepth is =< 0 and you try and add a mesh collider
            rsBackMf.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? _cachedSideMaterial : sideMaterial;
        
            // Check the underside is actually below the road
            if (sideDepth > 0)
            { 
                // Pull the bottom vertexes out of the left and right side vectors
                Pair<Vector3>[] underSideVectors = new Pair<Vector3>[leftSideVectors.Length];
                for (int i = 0; i < leftSideVectors.Length; i++)
                {
                    underSideVectors[i].First = leftSideVectors[i].Second;
                    underSideVectors[i].Second = rightSideVectors[i].First;
                }

                // Create the components for the underside of the road
                MeshFilter rsUnderMf = _underSide.AddComponent<MeshFilter>();
                _underSide.AddComponent<MeshRenderer>();
                rsUnderMf.mesh = Geometry.GenerateStrip(underSideVectors, transform, false, null, "RoadUnderside");
                _underSide.AddComponent<MeshCollider>().sharedMesh = rsUnderMf.sharedMesh;
                if (!sideMaterial) _underSide.GetComponent<Renderer>().sharedMaterial = _cachedSideMaterial;
                else _underSide.GetComponent<Renderer>().sharedMaterial = sideMaterial;
            }
        }

        /// <summary>
        /// Generate the MagnetRoad's SnapPoints
        /// </summary>
        /// <param name="spline">BezierSpline source</param>
        /// <param name="stepsPerCurve">Steps per curve (mesh resolution)</param>
        private void GenerateSnapPoints(BezierSpline spline, int stepsPerCurve)
        {
            // Check for existing snap points then see if they've moved, if so return
            if (SnapNodeLeft || SnapNodeRight)
            {
                if (SnapNodeLeft.position == splineSource.GetPoint(stepsPerCurve * spline.CurveCount) && SnapNodeRight.position == splineSource.GetPoint(0))
                    return;
            }

            // Destroy old snap points if needed and create new ones
            try
            {
                if (transform.Find("SnapNodeLeft") || transform.Find("SnapNodeRight"))
                {
                    if (transform.Find("SnapNodeLeft")) DestroyImmediate(transform.Find("SnapNodeLeft").gameObject); // destroy the old road pieces
                    if (transform.Find("SnapNodeRight")) DestroyImmediate(transform.Find("SnapNodeRight").gameObject);
                }
            }
            catch (Exception) { }
            Vector3 posLeft = spline.GetPoint(stepsPerCurve * spline.CurveCount);
            Vector3 posRight = spline.GetPoint(0f);
            _snapNodeLeft = new GameObject("SnapNodeLeft");
            _snapNodeRight = new GameObject("SnapNodeRight");
            _snapNodeLeft.transform.parent = gameObject.transform;
            _snapNodeLeft.AddComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Negative, roadWidth);
            _snapNodeLeft.transform.position = posLeft;
            _snapNodeRight.transform.parent = gameObject.transform;
            _snapNodeRight.AddComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Positive, roadWidth);
            _snapNodeRight.transform.position = posRight;
        }

        /// <summary>
        /// Method to update snap points without removing them 
        /// </summary>
        private void UpdateSnapPoints()
        {
            if (!_snapNodeLeft || !_snapNodeRight) GenerateSnapPoints(splineSource, stepsPerCurve);
            else
            {
                Vector3 posLeft = splineSource.GetPoint(stepsPerCurve * splineSource.CurveCount);
                Vector3 posRight = splineSource.GetPoint(0f);
                _snapNodeLeft.GetComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Negative, roadWidth);
                _snapNodeLeft.transform.position = posLeft;
                _snapNodeRight.GetComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Positive, roadWidth);
                _snapNodeRight.transform.position = posRight;
            }
        }

        /// <summary>
        /// Clear the mesh data for this MagnetRoad
        /// </summary>
        public void ClearRoadMesh()
        {
            try
            {
                if (_mesh) _mesh.Clear();
                _meshFilter.sharedMesh.Clear();
                _meshCollider.sharedMesh.Clear();
                if (_leftSide) DestroyImmediate(_leftSide);
                if (_rightSide) DestroyImmediate(_rightSide);
                if (_underSide) DestroyImmediate(_underSide);
            }
            catch (UnassignedReferenceException) // If can't find objects to clear, initialize them again
            {
                _meshFilter = GetComponent<MeshFilter>();
                _meshCollider = GetComponent<MeshCollider>();
                if (transform.childCount > 0)
                {
                    _leftSide = transform.Find("Road Side One").gameObject;
                    _rightSide = transform.Find("Road Side Two").gameObject;
                    _underSide = transform.Find("Road Underside").gameObject;
                }
                ClearRoadMesh();
            }
            catch (Exception e)
            {
                Debug.LogWarning("MESH FAILED TO CLEAR: " + e);
            }
        }

        /// <summary>
        /// Method to add a curve to the road
        /// </summary>
        public void AddCurve()
        {
            CleanupRuntimeHandles();
            splineSource.AddCurve(stepsPerCurve);
            InitializeRuntimeHandles();
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
        }

        /// <summary>
        /// Method to remove the most recently added curve
        /// </summary>
        public void RemoveCurve()
        {
            CleanupRuntimeHandles();
            splineSource.RemoveCurve();
            InitializeRuntimeHandles();
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
        }


        // Runtime Editor Methods

        /// <summary>
        /// Method to allow the user to edit the road at runtime
        /// </summary>
        public void EnableRuntimeEditing()
        {
            _isEditableAtRuntime = true;
            CleanupRuntimeHandles();
            InitializeRuntimeHandles();
        }

        /// <summary>
        /// Method to disable runtime editing at runtime
        /// </summary>
        public void DisableRuntimeEditing()
        {
            _isEditableAtRuntime = false;
            CleanupRuntimeHandles();
        }

        /// <summary>
        /// Intializer for the runtime editor handles
        /// </summary>
        private void InitializeRuntimeHandles()
        {
            if (_runtimeHandles.Length != splineSource.ControlPointCount && _isEditableAtRuntime)
            {
                // Generate runtime handles
                _runtimeHandles = new GameObject[splineSource.ControlPointCount];
                for (int i = 0; i < splineSource.ControlPointCount; i++)
                {
                    // Create point 'stems'
                    Vector3 position = splineSource.transform.TransformPoint(splineSource.GetControlPoint(i));
                    _runtimeHandles[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    _runtimeHandles[i].transform.position = new Vector3(position.x, position.y + roadWidth / 4, position.z);
                    _runtimeHandles[i].transform.localScale = new Vector3(roadWidth / 15, roadWidth / 4, roadWidth / 15);
                    _runtimeHandles[i].GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
                    _runtimeHandles[i].GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
                    _runtimeHandles[i].name = "RuntimeControlPin";

                    // Create selectable 'tops'
                    GameObject handleTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    handleTop.transform.position = new Vector3(position.x, position.y + roadWidth / 2, position.z);
                    handleTop.transform.localScale = new Vector3(roadWidth / 4, roadWidth / 4, roadWidth / 4);
                    handleTop.transform.parent = _runtimeHandles[i].transform;
                    handleTop.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
                    handleTop.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
                    handleTop.AddComponent<GizmoSelectable>();
                    handleTop.name = "RuntimeControlHandle" + i;
                    handleTop.transform.parent = transform;

                    _runtimeHandles[i].transform.parent = handleTop.transform;
                }

                // Create bezier curve line on road
                if (!GetComponent<LineRenderer>())
                {
                    _runtimeCurveLine = gameObject.AddComponent<LineRenderer>();
                }
                _runtimeCurveLine = GetComponent<LineRenderer>();
                _runtimeCurveLine.positionCount = (stepsPerCurve * splineSource.CurveCount + 1);
                Vector3[] temp = GetMiddleCarPath();
                for (int i = 0; i < temp.Length; i++) temp[i].y += 0.05f;
                _runtimeCurveLine.SetPositions(temp);
                _runtimeCurveLine.material = Resources.Load<Material>("Materials/_RuntimeGizmo/RoadLineColor");
                _runtimeCurveLine.sharedMaterial.color = Color.white;
                _runtimeCurveLine.startWidth = roadWidth/10;
                _runtimeCurveLine.transform.parent = transform;

                // Create Line Renderers for handle directions
                _runtimeHandleLines = null;
                _runtimeHandleLines = new LineRenderer[splineSource.ControlPointCount];
                for (int i = 1; i < splineSource.ControlPointCount; i += 3)
                {
                    CreateUpdateHandleLine(i - 1, i);
                    CreateUpdateHandleLine(i + 1, i + 2);
                }
            }
            else if (_runtimeHandles.Length > 1 && !_isEditableAtRuntime)
            {
                // Remove runtime handles & lines
                CleanupRuntimeHandles();
            }
        }

        /// <summary>
        /// Method to generate or update a handle line in-game
        /// </summary>
        /// <param name="handleIndex">Index of the handle you want to generate/update</param>
        /// <param name="lineIndex">Index of the line to draw</param>
        private void CreateUpdateHandleLine(int startHandleIndex, int lineIndex)
        {
            // Assert that the handle line is valid
            try
            {
                // Try to set the vertex count of this line
                _runtimeHandleLines[lineIndex].positionCount = 2;
            }
            catch (Exception)
            {
                // Operation failed, new LineRenderer needed  
                try
                {
                    // Try to set the vertex count again
                    _runtimeHandleLines[lineIndex] = _runtimeHandles[startHandleIndex].AddComponent<LineRenderer>();
                    _runtimeHandleLines[lineIndex].positionCount = 2; 
                }
                catch (Exception e)
                {
                    // line index invalid - only show during play mode
                    if (Application.isPlaying) Debug.LogError("ERROR ("+e+") when creating handle lines!");
                    return;
                }
            }

            // Update line data
            Vector3[] tempArray = new Vector3[2];
            Pair<Vector3> positions = new Pair<Vector3>();
            positions.First = splineSource.transform.TransformPoint(splineSource.GetControlPoint(startHandleIndex));
            positions.Second = splineSource.transform.TransformPoint(splineSource.GetControlPoint(startHandleIndex + 1));
            tempArray[0] = new Vector3(positions.First.x, positions.First.y + 0.05f, positions.First.z);
            tempArray[1] = new Vector3(positions.Second.x, positions.Second.y + 0.05f, positions.Second.z);
            _runtimeHandleLines[lineIndex].SetPositions(tempArray);
            _runtimeHandleLines[lineIndex].material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
            _runtimeHandleLines[lineIndex].sharedMaterial.color = Color.yellow;
            _runtimeHandleLines[lineIndex].startWidth = (roadWidth / 10);
        }

        /// <summary>
        /// Mehtod to update the spline's curve control points with the runtime handles
        /// </summary>
        private void UpdateRuntimeHandles()
        {
            if (_isEditableAtRuntime)
            {
                // Update any selected handles & the other handle positions
                for (int i = 0; i < splineSource.ControlPointCount; i += 1)
                {
                    RuntimeUpdatePoint(i);
                }

                // Update the line renderers
                if (_runtimeHandleLines.Length != splineSource.ControlPointCount) _runtimeHandleLines = new LineRenderer[splineSource.ControlPointCount];
                for (int i = 1; i < splineSource.ControlPointCount; i += 3)
                {
                    CreateUpdateHandleLine(i - 1, i);
                    CreateUpdateHandleLine(i + 1, i + 2);
                }

                // Update the curve line
                if (_runtimeCurveLine)
                {
                    if (_runtimeCurveLine.positionCount != (stepsPerCurve * splineSource.CurveCount + 1)) _runtimeCurveLine.positionCount = (stepsPerCurve * splineSource.CurveCount + 1);
                    Vector3[] temp = GetMiddleCarPath();
                    for (int i = 0; i < temp.Length; i++) temp[i].y += 0.05f;
                    _runtimeCurveLine.SetPositions(temp);
                }

                // Update the road geometry
                GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
            }
        }

        /// <summary>
        /// Method to update a spline point in realtime based on a given index
        /// </summary>
        /// <param name="index"></param>
        private void RuntimeUpdatePoint(int index)
        {
            try
            {
                Vector3 handlePos = _runtimeHandles[index].transform.position;
                Vector3 point = splineSource.transform.InverseTransformPoint(new Vector3(handlePos.x, handlePos.y - roadWidth / 4, handlePos.z));
                if (_gizmo != null)
                {
                    foreach (Transform objTransform in _gizmo.SelectedObjects)
                    {
                        if (objTransform == _runtimeHandles[index].transform.parent)
                        {
                            // Update the spline control point of the selected handle only
                            splineSource.SetControlPoint(index, point);
                            if (Application.isPlaying) RuntimeSnapPoint(index);
                            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
                        }
                        // Update the handle object position
                        _runtimeHandles[index].transform.parent.position = splineSource.transform.TransformPoint(splineSource.GetControlPoint(index) + new Vector3(0, (roadWidth / 2), 0));
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // The road handle likely no longer exists, this issue should fix itself without breaking
                // Occurs only when changing from 'Play' mode to 'Editor' when testing in Unity
            }
        }

        /// <summary>
        /// Method to handle the snapping of the road's magnet poles at runtime
        /// </summary>
        private void RuntimeSnapPoint(int selectedIndex)
        {
            // Get required vectors and arrays
            Vector3 selectedVector = splineSource.GetControlPoint(selectedIndex);
            MagnetRoad[] allRoads = FindObjectsOfType<MagnetRoad>();
            Intersection[] allIntersections = FindObjectsOfType<Intersection>();

            // Perform all snapping checks and finaly the snapping itself
            if  (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount -1) || selectedVector == splineSource.GetControlPoint(0))
            {
                foreach (MagnetRoad road in allRoads)
                {
                    Transform transformer = splineSource.transform;
                    SnapPoint pointA = splineSource.gameObject.GetComponent<MagnetRoad>().GetClosestSnapPointFromVector(transformer.TransformPoint(selectedVector));
                    SnapPoint pointB = road.GetClosestSnapPointFromVector(transformer.TransformPoint(selectedVector));
                    try
                    {
                        if (pointA.PointType != pointB.PointType)
                        {
                            if (road.gameObject != splineSource.gameObject)
                            {
                                if (Vector3.Distance(transformer.TransformPoint(selectedVector), road.SnapNodeLeft.position) <= road.roadWidth / 3)
                                {
                                    splineSource.SetControlPoint(selectedIndex, transformer.InverseTransformPoint(road.SnapNodeLeft.position));
                                    if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                    {
                                        float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                        splineSource.SetControlPoint(selectedIndex - 1, transformer.InverseTransformPoint(road.SnapNodeLeft.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    }
                                    if (selectedVector == splineSource.GetControlPoint(0))
                                    {
                                        float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(1)));
                                        splineSource.SetControlPoint(1, transformer.InverseTransformPoint(road.SnapNodeLeft.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    }
                                }
                                if (Vector3.Distance(transformer.TransformPoint(selectedVector), road.SnapNodeRight.position) <= road.roadWidth / 3)
                                {
                                    splineSource.SetControlPoint(selectedIndex, transformer.InverseTransformPoint(road.SnapNodeRight.position));
                                    if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                    {
                                        float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                        splineSource.SetControlPoint(selectedIndex - 1, transformer.InverseTransformPoint(road.SnapNodeRight.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    }
                                    if (selectedVector == splineSource.GetControlPoint(0))
                                    {
                                        float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(1)));
                                        splineSource.SetControlPoint(1, transformer.InverseTransformPoint(road.SnapNodeRight.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    }
                                }
                            }
                        }
                        pointA = null;
                        pointB = null;
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                    foreach (Intersection intersection in allIntersections)
                    {
                        foreach (SnapPoint snapNode in intersection.SnapNodes)
                        {
                            if (Vector3.Distance(transformer.TransformPoint(selectedVector), snapNode.transform.position) < intersection.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transformer.InverseTransformPoint(snapNode.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transformer.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    float distance = Vector3.Distance(transformer.TransformPoint(selectedVector), transformer.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transformer.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to remove any existing handles and lines
        /// </summary>
        private void CleanupRuntimeHandles()
        {
            if (!Application.isPlaying || !_isEditableAtRuntime)
            {
                DestroyImmediate(_runtimeCurveLine);
            }
            if (_runtimeHandles != null)
                if (_runtimeHandles.Length > 0)
                {
                    if (_gizmo)
                    {
                        _gizmo.ClearSelection();
                        _gizmo.Hide();
                    }
                    foreach (LineRenderer line in _runtimeHandleLines)
                    {
                        DestroyImmediate(line);
                    }
                    foreach (GameObject handle in _runtimeHandles)
                    {
                        if (handle) if (handle.transform.parent.gameObject) DestroyImmediate(handle.transform.parent.gameObject);
                        if (handle) DestroyImmediate(handle);
                    }
                    _runtimeHandles = new GameObject[0];
                }
        }


        // Unity Editor Methods

        /// <summary>
        /// Draw the MagnetRoad's mesh outline in-editor
        /// </summary>
        /// <param name="vertexData">Road vertex data</param>
        private void DrawRoadOutline(Pair<Vector3>[] vertexData)
        {
            Gizmos.color = new Color(1, 0.5f, 0.0f);
            Pair<Vector3> last, current;
            current = vertexData[0];
            Gizmos.DrawLine(current.First, current.Second);
            last = current;
            for (int i = 1; i <= vertexData.Length - 1; i++)
            {
                current = vertexData[i];
                Gizmos.DrawLine(current.First, current.Second);
                Gizmos.DrawLine(current.First, last.First);
                Gizmos.DrawLine(current.Second, last.Second);
                last = current;
            }
        }

        /// <summary>
        /// Draw the MagnetRoad's lanes in-editor
        /// </summary>
        /// <param name="pathData">Road's lane data</param>
        private void DrawCarPath(Pair<Vector3>[] pathData)
        {
            Gizmos.color = Color.blue;
            Pair<Vector3> last, current;
            last = pathData[0];
            for (int i = 1; i <= pathData.Length - 1; i++)
            {
                current = pathData[i];
                Gizmos.DrawLine(current.First, last.First);
                Gizmos.DrawLine(current.Second, last.Second);
                last = current;
            }
        }


        // Road Data Methods

        /// <summary>
        /// Return the central car path on this road
        /// </summary>
        /// <returns>An array of points following the center of the road</returns>
        public Vector3[] GetMiddleCarPath()
        {
            Pair<Vector3>[] carPathVectors = GenerateCarPathVectors(0);
            Vector3[] middleCarPath = new Vector3[carPathVectors.Length];
            for (int i = 0; i <= carPathVectors.Length - 1; i++)
            {
                middleCarPath[i] = carPathVectors[i].First;
            }
            return middleCarPath;
        }

        /// <summary>
        /// Return the left car path on this road
        /// </summary>
        /// <returns>An array of points following the left side of the road</returns>
        public Vector3[] GetLeftCarPath()
        {
            Pair<Vector3>[] carPathVectors = GenerateCarPathVectors(roadWidth);
            Vector3[] leftCarPath = new Vector3[carPathVectors.Length];
            for (int i = 0; i <= carPathVectors.Length - 1; i++)
            {
                leftCarPath[i] = carPathVectors[i].Second;
            }
            return leftCarPath;
        }

        /// <summary>
        /// Return the right car path on this road (inverted)
        /// </summary>
        /// <returns>An array of points following the right side of the road (inverted)</returns>
        public Vector3[] GetRightCarPath()
        {
            Pair<Vector3>[] carPathVectors = GenerateCarPathVectors(roadWidth);
            Vector3[] rightCarPath = new Vector3[carPathVectors.Length];
            for (int i = 0; i <= carPathVectors.Length - 1; i++)
            {
                rightCarPath[i] = carPathVectors[rightCarPath.Length - 1 - i].First;
            }
            return rightCarPath;
        }

        /// <summary>
        /// Return the closest SnapPoint on this road to a world coordinate
        /// </summary>
        /// <param name="vector">World coordinate</param>
        /// <returns>The closest SnapPoint</returns>
        public SnapPoint GetClosestSnapPointFromVector(Vector3 vector)
        {
            try
            {
                float distLeft = Vector3.Distance(vector, SnapNodeLeft.position);
                float distRight = Vector3.Distance(vector, SnapNodeRight.position);
                if (distLeft > distRight) return SnapNodeRight.gameObject.GetComponent<SnapPoint>();
                else if (distRight >= distLeft) return SnapNodeLeft.gameObject.GetComponent<SnapPoint>();
            }
            catch (Exception) { }
            return null;
        }


        // Road Saving

        /// <summary>
        /// Method to save this singular MagnetRoad to an XML file
        /// </summary>
        /// <param name="path"></param>
        public void SaveRoadToXML(string path = "DEFAULT_LOCATION")
        {
            try
            {
                MagnetRoadCollection collection = new MagnetRoadCollection();
                MagnetRoad[] magnetRoads = new MagnetRoad[1];
                magnetRoads[0] = this;
                collection.PrepareMagnetRoadData(magnetRoads);
                if (path == "DEFAULT_LOCATION") collection.Save(Path.Combine(Application.persistentDataPath, "RoadData.xml"));
                else collection.Save(path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Magnet Roads to a file, check the selected path.");
            }
        }

        /// <summary>
        /// Static method used to save all existing roads in the scene to a single XML file
        /// </summary>
        /// <param name="path">The file path to save to</param>
        public static void SaveRoadsToXML(string path = "DEFAULT_LOCATION")
        {
            try
            {
                MagnetRoadCollection collection = new MagnetRoadCollection();
                collection.PrepareMagnetRoadData(FindObjectsOfType<MagnetRoad>());
                collection.PrepareIntersectionData(FindObjectsOfType<Intersection>());
                if (path == "DEFAULT_LOCATION") collection.Save(Path.Combine(Application.persistentDataPath, "RoadData.xml"));
                else collection.Save(path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Magnet Roads to a file, check the selected path.");
            }
        }

        /// <summary>
        /// Static method used to load roads and intersections into the scene from an XML file
        /// </summary>
        /// <param name="path"></param>
        public static void LoadRoadsFromXML(string path)
        {
            // Store a list of recently spawned roads
            List<MagnetRoad> spawnedRoads = new List<MagnetRoad>();

            // Get the files
            string[] files = Directory.GetFiles(path);
            if (files.Length > 0)
            {
                foreach (string file in files)
                {
                    // Load the saved data
                    MagnetRoadCollection collection = MagnetRoadCollection.Load(file);

                    // Create saved Magnet Roads
                    var roadDataArray = collection.magnetRoadData;
                    if (collection.magnetRoadData != null)
                    {
                        foreach (MagnetRoadCollection.MagnetRoadData roadData in roadDataArray)
                        {
                            // Load the saved data into a new Magnet Road
                            MagnetRoad newMagnetRoad = new GameObject().AddComponent<MagnetRoad>();
                            newMagnetRoad.name = roadData.name;
                            newMagnetRoad.transform.position = roadData.location;
                            newMagnetRoad.transform.rotation = new Quaternion(roadData.rotation.x, roadData.rotation.y, roadData.rotation.z, 0.0f);
                            newMagnetRoad.transform.localScale = new Vector3(1, 1, 1);
                            newMagnetRoad.surfaceMaterial = (Material)Resources.Load("Materials/" + roadData.surfaceMaterial, typeof(Material));
                            newMagnetRoad.sideMaterial = (Material)Resources.Load("Materials/" + roadData.sideMaterial, typeof(Material));
                            newMagnetRoad.roadWidth = roadData.roadWidth;
                            newMagnetRoad.sideDepth = roadData.sideDepth;
                            newMagnetRoad.slopeWidth = roadData.slopeWidth;
                            newMagnetRoad.stepsPerCurve = roadData.stepsPerCurve;
                            if (roadData.isEditableAtRuntime)
                                newMagnetRoad.EnableRuntimeEditing();

                            // Create the req. number of curves for the road
                            while (roadData.handlePoints.Length > newMagnetRoad.splineSource.ControlPointCount)
                                newMagnetRoad.AddCurve();

                            // Place handle points in req. order
                            // Curve start & end points first
                            for (int i = 0; i < roadData.handlePoints.Length; i += 3) newMagnetRoad.splineSource.SetControlPoint(i, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i]));
                            // Then the mid points
                            for (int i = 1; i < roadData.handlePoints.Length - 1; i += 3)
                            {
                                newMagnetRoad.splineSource.SetControlPoint(i, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i]));
                                newMagnetRoad.splineSource.SetControlPoint(i + 1, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i + 1]));
                            }

                            // Generate the loaded road into the scene
                            newMagnetRoad.GenerateRoadMesh(newMagnetRoad.GenerateRoadVertexOutput(newMagnetRoad.roadWidth));
                            spawnedRoads.Add(newMagnetRoad);
                        }
                    }

                    // Create saved Intersections
                    var intersectionDataArray = collection.intersectionData;
                    if (collection.intersectionData != null)
                    {
                        foreach (MagnetRoadCollection.IntersectionData intersectionData in intersectionDataArray)
                        {
                            // Load the saved data into a new Intersection
                            Intersection newIntersection = new GameObject().AddComponent<Intersection>();
                            newIntersection.name = intersectionData.name;
                            newIntersection.transform.position = intersectionData.location;
                            newIntersection.transform.rotation = new Quaternion(intersectionData.rotation.x, intersectionData.rotation.y, intersectionData.rotation.z, 0);
                            newIntersection.transform.localScale = intersectionData.scale;
                            newIntersection.surfaceMaterial = (Material)Resources.Load("Materials/" + intersectionData.surfaceMaterial, typeof(Material));
                            newIntersection.sideMaterial = (Material)Resources.Load("Materials/" + intersectionData.sideMaterial, typeof(Material));
                            newIntersection.roadWidth = intersectionData.roadWidth;
                            newIntersection.sideDepth = intersectionData.sideDepth;
                            newIntersection.slopeWidth = intersectionData.slopeWidth;
                            if (intersectionData.isEditableAtRuntime)
                                newIntersection.EnableRuntimeEditing();

                            // Generate the loaded Intersection into the scene
                            newIntersection.SetUp(intersectionData.intersectionType);
                        }
                    }

                    // Check for possible snap points
                    if (spawnedRoads.Count > 0)
                    {
                        foreach (MagnetRoad road in spawnedRoads)
                        {
                            road.RuntimeSnapPoint(0);
                            road.RuntimeSnapPoint(road.splineSource.ControlPointCount - 1);
                        }
                    }

                }
            }
            else
            {
                // No files selected - return null
                Debug.LogWarning("No file(s) selected to load!");
            }
        }


        // Torchbearer MenuItem Methods

        /// <summary>
        /// Method to create a new MagnetRoad in the scene,
        /// </summary>
        /// <returns>The generated MagnetRoad's GameObject</returns>
        #if UNITY_EDITOR
        [MenuItem("Tools/Torchbearer Interactive/Magnet Roads/New Spline Road")]
        #endif
        public static GameObject CreateNewSplineRoad()
        {
            GameObject newOne = new GameObject();
            newOne.name = "Spline Road";
            newOne.AddComponent<MagnetRoad>();
            return newOne;
        }

        /// <summary>
        /// Method to create a new three-lane Intersection in the scene
        /// </summary>
        /// <returns>The generated Intersection's GameObject</returns>
        #if UNITY_EDITOR
        [MenuItem("Tools/Torchbearer Interactive/Magnet Roads/New Intersection/Three-lane")]
        #endif
        public static GameObject CreateNewThreeLane()
        {
            GameObject newOne = new GameObject();
            newOne.name = "Three-lane Intersection";
            newOne.AddComponent<Intersection>().SetUp(Intersection.IntersectionType.ThreeLane);
            return newOne;
        }

        /// <summary>
        /// Method to create a new four-lane Intersection in the scene
        /// </summary>
        /// <returns>The generated Intersection's GameObject</returns>
        #if UNITY_EDITOR
        [MenuItem("Tools/Torchbearer Interactive/Magnet Roads/New Intersection/Four-lane")] 
        #endif
        public static GameObject CreateNewFourLane()
        {
            GameObject newOne = new GameObject();
            newOne.name = "Four-lane Intersection";
            newOne.AddComponent<Intersection>().SetUp(Intersection.IntersectionType.FourLane);
            return newOne;
        }

        /// <summary>
        /// Method to create a new four-lane Intersection in the scene
        /// </summary>
        /// <returns>The generated Intersection's GameObject</returns>
        #if UNITY_EDITOR
        [MenuItem("Tools/Torchbearer Interactive/Magnet Roads/Save Current Road(s) as .xml", false, 99)]
        private static void SaveRoadsToFile()
        {
            // Get the desired file path
            string path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledRoads", "xml");

            // Try to save the roads
            try
            {
                SaveRoadsToXML(path);
            }
            catch (ArgumentException)
            {
                // No file selected
            }
        }
        #endif

        /// <summary>
        /// Method to create a new four-lane Intersection in the scene
        /// </summary>
        /// <returns>The generated Intersection's GameObject</returns>
        #if UNITY_EDITOR
        [MenuItem("Tools/Torchbearer Interactive/Magnet Roads/Load Road(s) from .xml", false, 99)]
        private static void LoadRoadsFromFile()
        {
            // Get the desired file path
            string path = EditorUtility.OpenFilePanel("Load Magnet Road XML file", "", "xml");

            // Load the roads
            try
            {
                LoadRoadsFromXML(path);
            }
            catch (ArgumentException)
            {
                // No file selected
            }
        }
        #endif
    }


    // This class handles the saving and loading of .xml files containing
    // MagnetRoad and Intersection instances

    /// <summary>
    /// MagnetRoad's collection class used for saving / loading
    /// </summary>
    [XmlRoot("MagnetRoadCollection")]
    public class MagnetRoadCollection
    {
        // Data Container Classes

        /// <summary>
        /// Container class for Magnet Road specific save data
        /// </summary>
        public class MagnetRoadData
        {
            [XmlAttribute("name")]
            public string name;
            public Vector3 location;
            public Vector3 rotation;
            public Vector3 scale;
            public string surfaceMaterial;
            public string sideMaterial;
            public float roadWidth;
            public float sideDepth;
            public float slopeWidth;
            public int stepsPerCurve;
            public bool isEditableAtRuntime;
            [XmlArray("HandlePoints")] [XmlArrayItem("HPoint")]
            public Vector3[] handlePoints;
        }

        /// <summary>
        /// Container class for Intersection specific save data
        /// </summary>
        public class IntersectionData
        {
            [XmlAttribute("name")]
            public string name;
            public Vector3 location;
            public Vector3 rotation;
            public Vector3 scale;
            public string surfaceMaterial;
            public string sideMaterial;
            public float roadWidth;
            public float sideDepth;
            public float slopeWidth;
            public bool isEditableAtRuntime;
            public Intersection.IntersectionType intersectionType;
        }


        // Data to Save

        /// <summary>
        /// Magnet Road data to save
        /// </summary>
        [XmlArray("MagnetRoads")] [XmlArrayItem("MagnetRoad")]
        public MagnetRoadData[] magnetRoadData;

        /// <summary>
        /// Intersection data to save
        /// </summary>
        [XmlArray("Intersections")] [XmlArrayItem("Intersection")]
        public IntersectionData[] intersectionData;


        // Data Preparation Methods 

        /// <summary>
        /// Prepare the Magnet Road data for saving
        /// </summary>
        /// <param name="input">Array of MagnetRoad objects to save</param>
        public void PrepareMagnetRoadData(MagnetRoad[] input)
        {
            magnetRoadData = new MagnetRoadData[input.Length];

            for(int i = 0; i < input.Length; i++)
            {
                magnetRoadData[i] = new MagnetRoadData();

                magnetRoadData[i].name = input[i].name;
                magnetRoadData[i].location = input[i].transform.position;
                magnetRoadData[i].rotation = input[i].transform.rotation.eulerAngles;
                magnetRoadData[i].scale = input[i].transform.localScale;
                magnetRoadData[i].surfaceMaterial = input[i].surfaceMaterial ? input[i].surfaceMaterial.name : null;
                magnetRoadData[i].sideMaterial = input[i].sideMaterial ? input[i].sideMaterial.name : null;
                magnetRoadData[i].roadWidth = input[i].roadWidth;
                magnetRoadData[i].sideDepth = input[i].sideDepth;
                magnetRoadData[i].slopeWidth = input[i].slopeWidth;
                magnetRoadData[i].stepsPerCurve = input[i].stepsPerCurve;
                magnetRoadData[i].isEditableAtRuntime = input[i].IsEditableAtRuntime;
                magnetRoadData[i].handlePoints = new Vector3[input[i].splineSource.ControlPointCount];
                for (int j = 0; j < input[i].splineSource.ControlPointCount; j++)
                {
                    magnetRoadData[i].handlePoints[j] = input[i].transform.TransformPoint(input[i].splineSource.GetControlPoint(j));
                }
            }
        }

        /// <summary>
        /// Prepare the Intersection data for saving
        /// </summary>
        /// <param name="input">Array of Intersection objects to save</param>
        public void PrepareIntersectionData(Intersection[] input)
        {
            intersectionData = new IntersectionData[input.Length];

            for(int i = 0; i < input.Length; i++)
            {
                intersectionData[i] = new IntersectionData();

                intersectionData[i].name = input[i].name;
                intersectionData[i].location = input[i].transform.position;
                intersectionData[i].rotation = input[i].transform.rotation.eulerAngles;
                intersectionData[i].scale = input[i].transform.localScale;
                intersectionData[i].surfaceMaterial = input[i].surfaceMaterial ? input[i].surfaceMaterial.name : null;
                intersectionData[i].sideMaterial = input[i].sideMaterial ? input[i].sideMaterial.name : null;
                intersectionData[i].roadWidth = input[i].roadWidth;
                intersectionData[i].sideDepth = input[i].sideDepth;
                intersectionData[i].slopeWidth = input[i].slopeWidth;
                intersectionData[i].isEditableAtRuntime = input[i].IsEditableAtRuntime;
                intersectionData[i].intersectionType = input[i].CurrentIntersectionType;
            }
        }


        // Saving & Loading

        /// <summary>
        /// Save the data to a .xml file
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            string testPath = path.Trim(' ');
            if (testPath == null || testPath == "") return;

            Debug.Log("Saving road(s) to " + path);
            XmlSerializer serializer = new XmlSerializer(typeof(MagnetRoadCollection));
            FileStream stream = new FileStream(path, FileMode.Create);
            try
            {
                serializer.Serialize(stream, this);
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Load the data from a .xml file 
        /// </summary>
        /// <param name="path">The path of the .xml file to load</param>
        /// <returns>MagnetRoadsCollection object</returns>
        public static MagnetRoadCollection Load(string path)
        {
            string testPath = path.Trim(' ');
            if (testPath == null || testPath == "") return null;

            Debug.Log("Loading road(s) from " + path);
            XmlSerializer serializer = new XmlSerializer(typeof(MagnetRoadCollection));
            FileStream stream = new FileStream(path, FileMode.Open);
            try
            {
                return serializer.Deserialize(stream) as MagnetRoadCollection;
            }
            finally
            {
                stream.Close();
            }
        }
    }


    // This class handles the editable attributes of the selected MagnetRoad
    // exposing them to the inspector

    /// <summary>
    /// MagnetRoad's custom inspector UI
    /// </summary>
    #if UNITY_EDITOR
    [CustomEditor(typeof(MagnetRoad))]
    public class SplineRoadEditorInspector : Editor
    {
        // Custom Inspector Data Variables

        /// <summary>
        /// Reference to this MangetRoad
        /// </summary>
        private MagnetRoad _road;

        /// <summary>
        /// Reference to the Torchbearer Logo
        /// </summary>
        private Texture _tbLogo;

        /// <summary>
        /// The speed the road followers will travel
        /// </summary>
        private float _roadFollowerSpeed;


        // Initialization Methods

        /// <summary>
        /// OnEnable load the inspector logo
        /// </summary>
        private void OnEnable()
        {
            _tbLogo = (Texture)Resources.Load("TBLogo", typeof(Texture));
        }

        /// <summary>
        /// Handle the Transform limitations in-editor
        /// </summary>
        public void OnSceneGUI()
        {
            // Hide the rotation & scaling controls on MagnetRoads
            if (_road = target as MagnetRoad)
            {
                if (Tools.current == Tool.Rotate || Tools.current == Tool.Scale) Tools.hidden = true;
                else Tools.hidden = false;
            }
            else Tools.hidden = false;
        }


        // GUI Display

        /// <summary>
        /// Render the MagnetRoad's custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            _road = target as MagnetRoad;
            GUILayout.Label(_tbLogo, GUILayout.Width(EditorGUIUtility.currentViewWidth - 40.0f), GUILayout.Height(60.0f));
            
            // Road editing
            GUILayout.Label("Road Data:", EditorStyles.boldLabel);
            DrawDefaultInspector();
            var oldColor = GUI.color;
            GUI.color = new Color(1, 0.5f, 0.0f);

            // Road generation
            if (_road.IsEditableAtRuntime) GUI.enabled = false;
            GUILayout.Label("Generation:", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate Road Mesh"))
            {
                Undo.RecordObject(_road, "Generate Road Mesh");
                EditorUtility.SetDirty(_road);
                _road.GenerateRoadMesh(_road.GenerateRoadVertexOutput(_road.roadWidth));
            }
            GUI.color = oldColor;
            if (GUILayout.Button("Clear Road Mesh"))
            {
                Undo.RecordObject(_road, "Clear Road Mesh");
                EditorUtility.SetDirty(_road);
                _road.ClearRoadMesh();
            }
            if (!GUI.enabled) GUI.enabled = true;
            
            // Curve editing
            GUILayout.Label("Extend / Shorten Road:", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Curve"))
            {
                Undo.RecordObject(_road.splineSource, "Add Curve");
                _road.AddCurve();
                EditorUtility.SetDirty(_road.splineSource);
            }
            if (GUILayout.Button("Remove Curve"))
            {
                Undo.RecordObject(_road.splineSource, "Remove Curve");
                _road.RemoveCurve();
                EditorUtility.SetDirty(_road.splineSource);
            }
            
            // Road testing 
            GUILayout.Label("Car Test:", EditorStyles.boldLabel);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("You cannot spawn road followers at runtime.", MessageType.Info);
                GUI.enabled = false;
            }
            bool custom = EditorGUILayout.Toggle("Use Custom Car", _road.customCar);
            _road.customCar = custom;
            if (_road.customCar)
            {
                var car = EditorGUILayout.ObjectField("Car Game Object", _road.carObject, typeof(GameObject), true);
                _road.carObject = (GameObject)car;
            }
            float speed = EditorGUILayout.FloatField("Follower Speed", _roadFollowerSpeed);
            _roadFollowerSpeed = speed;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Left Lane", EditorStyles.miniButtonLeft))
            {
                if (_road.customCar && _road.carObject != null)
                {
                    DebugSpawnFollower(_road.carObject, _road.GetLeftCarPath()); // spawn custom car
                }
                else
                {
                    DebugSpawnFollower(_road.GetLeftCarPath()); // spawn cube follower
                }
            }
            if (GUILayout.Button("Central", EditorStyles.miniButtonMid))
            {
                if (_road.customCar && _road.carObject != null)
                {
                    DebugSpawnFollower(_road.carObject, _road.GetMiddleCarPath()); // spawn custom car
                }
                else
                {
                    DebugSpawnFollower(_road.GetMiddleCarPath()); // spawn cube follower
                }
            }
            if (GUILayout.Button("Right Lane", EditorStyles.miniButtonRight))
            {
                if (_road.customCar && _road.carObject != null)
                {
                    DebugSpawnFollower(_road.carObject, _road.GetRightCarPath()); // spawn custom car
                }
                else
                {
                    DebugSpawnFollower(_road.GetRightCarPath()); // spawn cube follower
                }
            }
            if (!GUI.enabled) GUI.enabled = true;
            GUILayout.EndHorizontal();

            // Save road
            GUILayout.Label("Save:", EditorStyles.boldLabel);
            GUI.color = new Color(.2f,.55f,1);
            if (GUILayout.Button("Save Selected Road to XML"))
            {
                string path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledRoad", "xml");
                try
                {
                    _road.SaveRoadToXML(path);
                }
                catch (ArgumentException)
                {
                    // No folder selected - ignore
                }
            }
        }


        // Road Follower Methods

        /// <summary>
        /// Spawns a Road Follower
        /// </summary>
        /// <param name="source">Custom Road Follower</param>
        /// <param name="roadPath">Path to follow</param>
        /// <returns>The new Road Follower</returns>
        private GameObject DebugSpawnFollower(GameObject source, Vector3[] roadPath)
        {
            GameObject temp = Instantiate(source);
            temp.AddComponent<RoadFollower>();
            float speed = _roadFollowerSpeed <= 0 ? 1.0f : _roadFollowerSpeed; 
            temp.GetComponent<RoadFollower>().SetupRoadFollower(roadPath, speed);
            return temp;
        }

        /// <summary>
        /// Spawns a Road Follower
        /// </summary>
        /// <param name="roadPath">Path to follow</param>
        /// <returns>The new Road Follower</returns>
        private GameObject DebugSpawnFollower(Vector3[] roadPath)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.transform.localScale = new Vector3(_road.roadWidth/2, _road.roadWidth / 2, _road.roadWidth / 2);
            temp.AddComponent<RoadFollower>();
            float speed = _roadFollowerSpeed <= 0 ? 1.0f : _roadFollowerSpeed;
            temp.GetComponent<RoadFollower>().SetupRoadFollower(roadPath, speed);
            return temp;
        }
    }
    #endif
 }
