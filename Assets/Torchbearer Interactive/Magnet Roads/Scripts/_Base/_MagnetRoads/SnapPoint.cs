// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************

// This class is used for the sole purpose of indicating, in-editor, the locations of snappable points
// on existing SplineRoads

using UnityEngine;
using System; 

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MagnetRoads (base)
/// </summary>
namespace MagnetRoads
{
    /// <summary>
    /// MagnetRoads (base)
    /// </summary>
    [ExecuteInEditMode] [SelectionBase] [AddComponentMenu("")]
    public class SnapPoint : MonoBehaviour
    {
        // Point Data Variables

        /// <summary>
        /// This SnapPoint's polarity
        /// </summary>
        [HideInInspector] [SerializeField]
        private PointEnd _pointEnd;

        /// <summary>
        /// This source road's width
        /// </summary>
        [HideInInspector] [SerializeField]
        private float _roadWidth;

        /// <summary>
        /// Accessor for the point's polarity
        /// </summary>
        public PointEnd PointType { get { return _pointEnd; } }

        /// <summary>
        /// Editable road's magnet point
        /// </summary>
        private GameObject _inEditorMagnetPoint = null;
        

        // Enums

        /// <summary>
        /// Magnet polarity data
        /// </summary>
        public enum PointEnd
        {
            Positive,
            Negative,
            Bipolar
        }


        // Initialization Methods

        /// <summary>
        /// Set-up this SnapPoint
        /// </summary>
        /// <param name="pointType">The polarity of the point to set-up</param>
        /// <param name="roadWidth">The source road's width</param>
        public void SetUp(PointEnd pointType, float roadWidth)
        {
            _pointEnd = pointType;
            _roadWidth = roadWidth;
        }


        // Update Methods

        /// <summary>
        /// Per frame update loop, update snap point visuals
        /// </summary>
        private void Update()
        {
            // Handle the spawning & clearing of the runtime snap points
            if (Application.isPlaying && IsAttachedRoadEditable())
            {
                // Set up in-editor handle if not already done
                if (!_inEditorMagnetPoint) _inEditorMagnetPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (_inEditorMagnetPoint.transform.parent != transform) _inEditorMagnetPoint.transform.parent = transform;
                _inEditorMagnetPoint.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                _inEditorMagnetPoint.name = "__RuntimeSnapNode";

                // Handle node type styling
                if (!_inEditorMagnetPoint.GetComponent<Renderer>().enabled) _inEditorMagnetPoint.GetComponent<Renderer>().enabled = true;
                switch (PointType)
                {
                    case PointEnd.Positive:
                        _inEditorMagnetPoint.transform.localScale = new Vector3(_roadWidth / 1.4f, _roadWidth / 24, _roadWidth / 1.4f);
                        _inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Positive");
                        break;

                    case PointEnd.Negative:
                        _inEditorMagnetPoint.transform.localScale = new Vector3(_roadWidth / 1.4f - 0.01f, _roadWidth / 24, _roadWidth / 1.4f - 0.01f);
                        _inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Negative");
                        break;

                    case PointEnd.Bipolar:
                        _inEditorMagnetPoint.transform.localScale = new Vector3(_roadWidth / 1.4f - 0.02f, _roadWidth / 24, _roadWidth / 1.4f - 0.02f);
                        _inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Bipolar");
                        break;
                }
            }
            else
            {
                if (_inEditorMagnetPoint) DestroyImmediate(_inEditorMagnetPoint);
            }
        }

        /// <summary>
        /// Draw the in-editor giszmos for this SnapPoint
        /// </summary>
        private void OnDrawGizmos()
        {
            if (PointType == PointEnd.Positive)
            {
                Gizmos.color = new Color(1, 0.5f, 0.0f);
                var offset = new Vector3(_roadWidth / 3.5f, 0, 0);
                Gizmos.DrawLine(transform.position - offset, transform.position + offset);
                offset = new Vector3(0, 0, _roadWidth / 3.5f);
                Gizmos.DrawLine(transform.position - offset, transform.position + offset);
                Gizmos.DrawCube(transform.position, new Vector3(0.05f, 0.05f, 0.05f));

				#if UNITY_EDITOR
				Handles.color = new Color(1, 0.5f, 0.0f);
				Handles.DrawWireDisc(transform.position, Vector3.up, _roadWidth / 3.5f);
				Handles.DrawSolidDisc(transform.position, Vector3.up, _roadWidth / 8f);
                #endif
            }
            if (PointType == PointEnd.Negative)
            {
                Gizmos.color = Color.blue;
                var offset = new Vector3(_roadWidth / 3.5f, 0, 0);
                Gizmos.DrawLine(transform.position - offset, transform.position + offset);
				Gizmos.DrawCube(transform.position, new Vector3(0.05f, 0.05f, 0.05f));

				#if UNITY_EDITOR
				Handles.color = Color.blue;
                Handles.DrawWireDisc(transform.position, Vector3.up, _roadWidth / 3.5f);
                Handles.DrawSolidDisc(transform.position, Vector3.up, _roadWidth / 8f);
                #endif
            }
            if (PointType == PointEnd.Bipolar)
            {
				#if UNITY_EDITOR
                Handles.color = Color.white;
                Handles.DrawWireDisc(transform.position, Vector3.up, _roadWidth / 3f);
                #endif
            }
        }


        // Getters

        /// <summary>
        /// Check if this Snap Point is attached to an editable road
        /// </summary>
        /// <returns>True if attached road is editable in-game</returns>
        private bool IsAttachedRoadEditable()
        {
            try
            {
                if (transform.parent.GetComponent<MagnetRoad>()) return transform.parent.GetComponent<MagnetRoad>().IsEditableAtRuntime;
                if (transform.parent.parent.GetComponent<Intersection>()) return transform.parent.parent.GetComponent<Intersection>().IsEditableAtRuntime;
                return false;
            }
            catch (Exception)
            {
                return false; // invalid parent
            }
        }
    }


    #if UNITY_EDITOR
    // This class handles the custom inspector of a SnapPoint using it's stored
    // polarity to display specific information to the user

    /// <summary>
    /// Custom inspector UI for the SnapPoints
    /// </summary>
    [CustomEditor(typeof(SnapPoint))]
    public class SnapPointInspector : Editor
    {
        /// <summary>
        /// Reference to the source SnapPoint
        /// </summary>
        private SnapPoint _snapPoint;

        /// <summary>
        /// Draw the SnapPoint's custom inspector UI
        /// </summary>
        public override void OnInspectorGUI()
        {
            _snapPoint = target as SnapPoint;
            DrawDefaultInspector();
            try
            {
                if (_snapPoint.transform.parent.GetComponent<MagnetRoad>())
                {
                    EditorGUILayout.HelpBox("Do not use the Snap Points to manipulate the spline, click the road itself and make use of the yellow handles.", MessageType.Warning);
                }
                if (_snapPoint.transform.parent.GetComponent<Intersection>())
                {
                    EditorGUILayout.HelpBox("This is a Bipolar Snap Point, it will accept road ends of any polarity.", MessageType.Info);
                }
            }
            catch (NullReferenceException)
            {
                EditorGUILayout.HelpBox("This Snap Point must be a child of a relevant game object to function properly!", MessageType.Error);
            }
        }
    }
	#endif
}