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

// Add this class to objects you want to be able to move in-game!

// WARNING! Owing to the implementation of MagnetRoads we advise that you
// do not apply this script to any objects outside of magnet roads as the
// selectabilty of objects is directly tied into the functionality of the
// runtime magnet snapping. 

using UnityEngine;
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
    public class GizmoSelectable : MonoBehaviour
    {
        /// <summary>
        /// Static reference to Gizmo in scene
        /// </summary>
        private static Gizmo s_gizmoControl;

        /// <summary>
        /// Detects weather shift is pressed
        /// </summary>
        private bool shiftDown;

        /// <summary>
        /// Initialization method
        /// </summary>
        void Start()
        {
            if (FindObjectOfType<Gizmo>())
            {
                s_gizmoControl = FindObjectOfType<Gizmo>();
            }
            else
            {
                if (GameObject.Find("__RuntimeGizmo")) DestroyImmediate(GameObject.Find("__RuntimeGizmo"));
                var temp = Instantiate(Resources.Load("_Base/_RuntimeGizmo/Gizmo") as GameObject);
                s_gizmoControl = temp.GetComponent<Gizmo>();
                s_gizmoControl.transform.rotation = Quaternion.Euler(0,90,0);
                s_gizmoControl.name = "__RuntimeGizmo";
            }
         }

        /// <summary>
        /// Check for mouse click
        /// </summary>
        void OnMouseDown()
        {
            if (s_gizmoControl != null)
            {
                if (!shiftDown)
                {
                    s_gizmoControl.ClearSelection();
                }
                s_gizmoControl.Show();
                s_gizmoControl.SelectObject(transform);
                gameObject.layer = 2;
            }
        }
        
        /// <summary>
        /// Per frame update loop
        /// </summary>
        void Update()
        {
            shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ? true : false;
        }

        /// <summary>
        /// Deseclect this object
        /// </summary>
        public void Unselect()
        {
            gameObject.layer = 0;
        }
    }
}