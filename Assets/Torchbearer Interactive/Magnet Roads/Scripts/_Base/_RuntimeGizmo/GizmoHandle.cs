// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************
// Custom gizmo is inspired by, but unsourced from: 
// https://forum.unity3d.com/threads/in-game-gizmo-handle-control.154948/
// ************************************************************************

using UnityEngine;
using TBUnityLib.Generic; 
using System.Collections;

/// <summary>
/// (Runtime Gizmo)
/// </summary>
namespace RuntimeGizmo
{
    /// <summary>
    /// (Runtime Gizmo)
    /// </summary>
    [AddComponentMenu("")]
    public class GizmoHandle : MonoBehaviour
    {
        // Gizmo Data
        public Gizmo gizmo;
        public GizmoControl gizmoController;
        public GizmoTypes gizmoType;

        // Handle Visual Objects
        public GameObject PositionCap;
        public GameObject RotationCap;
        public GameObject ScaleCap;
        public Material ActiveMaterial;

        // Handle Data
        public GizmoAxis gizmoAxis;
        public float mouseSensitivity = 10f;
        public float rotationSensitivity = 0f;
        public float scaleSensitivity = 0f;
        private Material inactiveMaterial;
        private bool activeHandle;

        // Offset Data
        private Vector3 _gizmoMouseOffset;
        private Vector3 _gizmoStartPosition;

        /// <summary>
        /// OnAwake method
        /// </summary>
        void Awake()
        {
            inactiveMaterial = GetComponent<Renderer>().material;
        }

        /// <summary>
        /// When mouse click
        /// </summary>
        public void OnMouseDown()
        {
            _gizmoMouseOffset = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(gizmo.transform.position).z));
            _gizmoStartPosition = gizmo.transform.position;

            gizmo.DeactivateHandles();
            SetActive(true);
        }

        /// <summary>
        /// When the player tries to drag the object
        /// </summary>
        public void OnMouseDrag()
        {
            float distanceToScreen = Camera.main.WorldToScreenPoint(gizmo.transform.position).z;
            if (activeHandle)
            {

                switch (gizmoType)
                {
                    case GizmoTypes.Position:
                        Vector3 posMove = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceToScreen));
                        Vector3 roadCenter = Vector3.zero;
                        switch (gizmoAxis)
                        {
                            case GizmoAxis.X:
                                foreach (Transform obj in gizmo.SelectedObjects)
                                {
                                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                                    obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, posMove.z - ((_gizmoMouseOffset.z - _gizmoStartPosition.z)-roadCenter.z));
                                }
                                break;
                            case GizmoAxis.Y:
                                foreach (Transform obj in gizmo.SelectedObjects)
                                {
                                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                                    obj.transform.position = new Vector3(obj.transform.position.x, posMove.y - ((_gizmoMouseOffset.y - _gizmoStartPosition.y)-roadCenter.y), obj.transform.position.z);
                                }
                                break;
                            case GizmoAxis.Z:
                                foreach (Transform obj in gizmo.SelectedObjects)
                                {
                                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                                    obj.transform.position = new Vector3((posMove.x - (_gizmoMouseOffset.x - _gizmoStartPosition.x)+roadCenter.x), obj.transform.position.y, obj.transform.position.z);
                                }
                                break;
                            case GizmoAxis.Center:
                                foreach (Transform obj in gizmo.SelectedObjects)
                                {
                                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                                    obj.transform.position = new Vector3(posMove.x + roadCenter.x, obj.transform.position.y, posMove.z + roadCenter.z);
                                }
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Set this handle to be active or inactive in the scene
        /// </summary>
        /// <param name="active">active state</param>
        public void SetActive(bool active)
        {
            if (active)
            {
                activeHandle = true;
                GetComponent<Renderer>().material = ActiveMaterial;
                if (gizmoAxis != GizmoAxis.Center)
                {
                    PositionCap.GetComponent<Renderer>().material = ActiveMaterial;
                    RotationCap.GetComponent<Renderer>().material = ActiveMaterial;
                    ScaleCap.GetComponent<Renderer>().material = ActiveMaterial;
                }
            }
            else
            {
                activeHandle = false;
                GetComponent<Renderer>().material = inactiveMaterial;
                if (gizmoAxis != GizmoAxis.Center)
                {
                    PositionCap.GetComponent<Renderer>().material = inactiveMaterial;
                    RotationCap.GetComponent<Renderer>().material = inactiveMaterial;
                    ScaleCap.GetComponent<Renderer>().material = inactiveMaterial;
                }
            }
        }

        /// <summary>
        /// Set the transform type of this gizmo (ONLY POSITION IS SUPPORTED FOR MAGNET ROADS)
        /// </summary>
        /// <param name="type"></param>
        public void SetType(GizmoTypes type)
        {
            gizmoType = type;
            if (gizmoAxis != GizmoAxis.Center)
            {
                Helper.SetActiveRecursively(PositionCap, type == GizmoTypes.Position);
                Helper.SetActiveRecursively(RotationCap, type == GizmoTypes.Rotation);
                Helper.SetActiveRecursively(ScaleCap, type == GizmoTypes.Scale);
            }
        }
    }
}