// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************
// Majority of custom gizmo is inspired by, but unsourced from: 
// https://forum.unity3d.com/threads/in-game-gizmo-handle-control.154948/
// ************************************************************************

using UnityEngine;
using TBUnityLib.Generic; 
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// (Runtime Gizmo)
/// </summary>
namespace RuntimeGizmo
{
    /// <summary>
    /// Types of gizmos avaiable (we only use position)
    /// </summary>
    public enum GizmoTypes { Position, Rotation, Scale }


    /// <summary>
    /// Types of gizmo control axis
    /// </summary>
    public enum GizmoControl { Horizontal, Vertical, Both }


    /// <summary>
    /// Selected gizmo axis
    /// </summary>
    public enum GizmoAxis { Center, X, Y, Z }
    

    /// <summary>
    /// (Runtime Gizmo)
    /// </summary>
    [AddComponentMenu("")]
    public class Gizmo : MonoBehaviour
    {
        // Gizmo data
        public GizmoHandle AxisCenter;
        public GizmoHandle AxisX;
        public GizmoHandle AxisY;
        public GizmoHandle AxisZ;
        public GizmoTypes Type;
        public List<Transform> SelectedObjects;
        public Vector3 Center;
        public Camera Camera;
        public bool Visible;
        public float DefaultDistance = 3.2f;
        public float ScaleFactor = 0.2f;
        private Vector3 localScale;
        private Transform _transform;

        /// <summary>
        /// Initialization method
        /// </summary>
        void Awake()
        {
            Visible = false;
            SetType(GizmoTypes.Position);
            Hide();
            // set the axis start type
            AxisCenter.gizmoAxis = GizmoAxis.Center;
            AxisCenter.gizmo = this;
            AxisX.gizmoAxis = GizmoAxis.X;
            AxisX.gizmo = this;
            AxisY.gizmoAxis = GizmoAxis.Y;
            AxisY.gizmo = this;
            AxisZ.gizmoAxis = GizmoAxis.Z;
            AxisZ.gizmo = this;

            _transform = transform;
            localScale = _transform.localScale;
            SelectedObjects = new List<Transform>();
        }

        /// <summary>
        /// Per frame update loop
        /// </summary>
        void Update()
        {
            if (Visible)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SetType(GizmoTypes.Position);
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ClearSelection();
                    Hide();
                }
            }
            if (SelectedObjects.Count > 0)
            {
                // Scale based on distance from the camera
                var distance = Vector3.Distance(_transform.position, FindObjectOfType<Camera>().transform.position);
                var scale = (distance - DefaultDistance) * ScaleFactor;
                _transform.localScale = new Vector3(localScale.x + scale, localScale.y + scale, localScale.z + scale);

                // Move the gizmo to the center of our parent
                UpdateCenter();
                _transform.position = Center;
            }
        }

        /// <summary>
        /// Set this handle's transform type
        /// </summary>
        /// <param name="type">transform type</param>
        public void SetType(GizmoTypes type)
        {
            // set the type of all the axis
            Type = type;
            AxisCenter.SetType(type);
            AxisX.SetType(type);
            AxisY.SetType(type);
            AxisZ.SetType(type);
        }

        /// <summary>
        /// Clear the currently selected objects
        /// </summary>
        public void ClearSelection()
        {
            foreach (var obj in SelectedObjects)
            {
                (obj.gameObject.GetComponent<GizmoSelectable>()).Unselect();
            }
            SelectedObjects.Clear();
            Center = Vector3.zero;
        }

        /// <summary>
        /// Update the center of 
        /// </summary>
        public void UpdateCenter()
        {
            if (SelectedObjects.Count > 1)
            {
                var vectors = new Vector3[SelectedObjects.Count];
                for (int i = 0; i < SelectedObjects.Count; i++)
                {
                    // Check for a Magnet Road
                    if (SelectedObjects[i].GetComponent<MagnetRoads.MagnetRoad>())
                    {
                        vectors[i] = SelectedObjects[i].GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                    }
                    else
                    {
                        vectors[i] = SelectedObjects[i].position;
                    }
                }
                Center = GetCenterOfVectors(vectors);
            }
            else
            {
                if (SelectedObjects[0]) 
                    if (SelectedObjects[0].GetComponent<MagnetRoads.MagnetRoad>())
                    {
                        Center = SelectedObjects[0].GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                        return;
                    }
                    else
                    {
                        Center = SelectedObjects[0].position;
                    }
            }
        }

        /// <summary>
        /// Select an object in the scene (must be selectable)
        /// </summary>
        /// <param name="parent">parent transform</param>
        public void SelectObject(Transform parent)
        {
            if (!SelectedObjects.Contains(parent))
                SelectedObjects.Add(parent);
            UpdateCenter();
        }

        /// <summary>
        /// Activate an exis for manipulation
        /// </summary>
        /// <param name="axis">Axis to activate</param>
        public void ActivateAxis(GizmoAxis axis)
        {
            switch (axis)
            {
                case GizmoAxis.Center:
                    AxisCenter.SetActive(true);
                    break;
                case GizmoAxis.X:
                    AxisX.SetActive(true);
                    break;
                case GizmoAxis.Y:
                    AxisY.SetActive(true);
                    break;
                case GizmoAxis.Z:
                    AxisZ.SetActive(true);
                    break;
            }
            SetType(Type);
        }

        /// <summary>
        /// Deactivate an axis from any further interaction
        /// </summary>
        /// <param name="axis">Axis to deactivate</param>
        public void DeactivateAxis(GizmoAxis axis)
        {
            switch (axis)
            {
                case GizmoAxis.Center:
                    AxisCenter.SetActive(false);
                    break;
                case GizmoAxis.X:
                    AxisX.SetActive(false);
                    break;
                case GizmoAxis.Y:
                    AxisY.SetActive(false);
                    break;
                case GizmoAxis.Z:
                    AxisZ.SetActive(false);
                    break;
            }
            SetType(Type);
        }

        /// <summary>
        /// Deactivate the handle controls
        /// </summary>
        public void DeactivateHandles()
        {
            AxisCenter.SetActive(false);
            AxisX.SetActive(false);
            AxisY.SetActive(false);
            AxisZ.SetActive(false);
        }

        /// <summary>
        /// Show the gizmo
        /// </summary>
        public void Show()
        {
            Helper.SetActiveRecursively(gameObject, true);
            SetType(Type);
            Visible = true;
        }

        /// <summary>
        /// Hide the gizmo
        /// </summary>
        public void Hide()
        {
            Helper.SetActiveRecursively(gameObject, false);
            gameObject.SetActive(true);
            Visible = false;
        }

        /// <summary>
        /// Get the center of all the selected objects
        /// </summary>
        /// <param name="vectors">Array of selected vectors</param>
        /// <returns>The center point of the provided vectors</returns>
        public Vector3 GetCenterOfVectors(Vector3[] vectors)
        {
            Vector3 sum = Vector3.zero;
            if (vectors == null || vectors.Length == 0)
            {
                return sum;
            }

            foreach (Vector3 vec in vectors)
            {
                sum += vec;
            }
            return sum / vectors.Length;
        }
    }
}
