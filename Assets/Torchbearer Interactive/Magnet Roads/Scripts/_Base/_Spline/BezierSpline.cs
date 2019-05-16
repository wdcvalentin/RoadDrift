// ************************************************************************
// Majority of bezier & spline code is open source and availiable from: 
// http://catlikecoding.com/unity/tutorials/curves-and-splines/
// ************************************************************************
//
// This source file has been heavily modified from the original source
// Re-implementing this code as intended in the original source material
// may not work as expected
//
// Additions include: 
// + Methods to extrapolate specifically offset vectors from the spline
// + Major addition of point snapping added to the inspector code which
//   makes extensive use of the SnapPoint class
//
// ************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
//
// Unauthorized copying of this file, via any medium is strictly prohibited
// proprietary and confidential
// 
// Written by: Jonathan H Langley - jon@tbinteractive.co.uk, 2017
// ************************************************************************

using UnityEngine;
using MagnetRoads;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// (Bezier Spline)
/// </summary>
namespace BezierSplines
{
    /// <summary>
    /// (Bezier Spline)
    /// </summary>
    public enum BezierControlPointMode
    {
        Free,
        Aligned,
        Mirrored
    }


    /// <summary>
    /// (Bezier Spline)
    /// </summary>
    [AddComponentMenu("")] // hide from add component menu
    public class BezierSpline : MonoBehaviour
    {

        [SerializeField]
        private BezierControlPointMode[] modes;

        [SerializeField]
        private Vector3[] points;

        public int CurveCount { get { return (points.Length - 1) / 3; } }

        public int ControlPointCount { get { return points.Length; } }

        public Vector3 GetControlPoint(int index) { return points[index]; }

        public void Awake()
        {
            if (points == null) Reset();
        }

        public void SetControlPoint(int index, Vector3 point)
        {
            try
            {
                points[index].ToString(); // test the vector is active
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogException(e); // null index value
            }
            if (index % 3 == 0)
            {
                // if we're adjusting the position of a centre point, then we should also adjust the position of the
                // neighbouring points too/
                Vector3 centrePointPositionOffset = point - points[index];
                if (index > 0)
                {
                    points[index - 1] += centrePointPositionOffset;
                }
                if (index + 1 < points.Length)
                {
                    points[index + 1] += centrePointPositionOffset;
                }
            }
            points[index] = point;
            EnforceMode(index);
        }

        public void Reset()
        {
            points = new Vector3[] 
            {
                new Vector3(-1.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(1.5f, 0f, 0f)
            };
            modes = new BezierControlPointMode[] 
            {
                BezierControlPointMode.Aligned,
                BezierControlPointMode.Aligned
            };
        }

        public void AddCurve(int stepsPerCurve)
        {
            Vector3 point = points[points.Length - 1];
            Vector3 direction = transform.InverseTransformDirection(GetDirection(points.Length - 1));
            System.Array.Resize(ref points, points.Length + 3);

            point += direction * 1f;
            points[points.Length - 3] = point;
            point += direction * 1f;
            points[points.Length - 2] = point;
            point += direction * 1f;
            points[points.Length - 1] = point;

            System.Array.Resize(ref modes, modes.Length + 1);
            modes[modes.Length - 1] = modes[modes.Length - 2];
            EnforceMode(points.Length - 4);
        }

        public void RemoveCurve()
        {
            if (points.Length > 4)
            {
                System.Array.Resize(ref points, points.Length - 3);
            }
        }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        public Vector3 GetRotation(float t)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, 0);
            return rotation * transform.InverseTransformDirection(GetDirection(t));
        }

        // CUSTOM METHOD:
        // Returns an offset from a point on the curve based on a supplied rotation
        public Vector3 GetOffsetRotation(float t, Quaternion rotation)
        {
            return rotation * transform.InverseTransformDirection(GetDirection(t));
        }

        private void EnforceMode(int index)
        {
            int modeIndex = (index + 1) / 3;
            BezierControlPointMode mode = modes[modeIndex];
            if (mode == BezierControlPointMode.Free || modeIndex == 0 || modeIndex == modes.Length - 1)
            {
                return;
            }
            int middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (index <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                enforcedIndex = middleIndex + 1;
            }
            else
            {
                fixedIndex = middleIndex + 1;
                enforcedIndex = middleIndex - 1;
            }
            Vector3 middle = points[middleIndex];
            Vector3 enforcedTangent = middle - points[fixedIndex];
            if (mode == BezierControlPointMode.Aligned && enforcedIndex <= points.Length - 1)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
            }
            if (enforcedIndex <= points.Length - 1) points[enforcedIndex] = middle + enforcedTangent;
        }

        public BezierControlPointMode GetControlPointMode(int index) { return modes[(index + 1) / 3]; }

        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {
            modes[(index + 1) / 3] = mode;
            EnforceMode(index);
        }
    }


    /// <summary>
    /// (Bezier Spline)
    /// </summary>
	#if UNITY_EDITOR
    [CustomEditor(typeof(BezierSpline))]
    public class BezierSplineEditorInspector : Editor
    {

        private BezierSpline _spline;
        private GameObject _gameObject;
        private Transform _handleTransform;
        private Quaternion _handleRotation;

        private const int _stepsPerCurve = 10;
        private const float _directionScale = 0.5f;
        private const float _handleSize = 0.08f;
        private const float _pickSize = 0.1f;

        private int _selectedIndex = -1;

        private static Color[] modeColors = {
            Color.white,
            Color.yellow,
            Color.cyan
        };

        public override void OnInspectorGUI()
        {
            _spline = target as BezierSpline;
            GUILayout.Label("Curve Point Editor:", EditorStyles.boldLabel);
            if (_selectedIndex >= 0 && _selectedIndex < _spline.ControlPointCount)
            {
                DrawSelectedPointInspector();
                SnapFirstAndLastPoints_Inspector();
            }
            else
            {
                GUILayout.Label("      NO CURVE POINT SELECTED", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawSelectedPointInspector()
        {
            EditorGUI.BeginChangeCheck();
            var point = EditorGUILayout.Vector3Field("Position", _spline.GetControlPoint(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Move Point");
                EditorUtility.SetDirty(_spline);
                _spline.SetControlPoint(_selectedIndex, point);
            }
            EditorGUI.BeginChangeCheck();
            var mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Alignment Mode", _spline.GetControlPointMode(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Change Point Alignment Mode");
                _spline.SetControlPointMode(_selectedIndex, mode);
                EditorUtility.SetDirty(_spline);
            }
        }

        // CUSTOM METHOD:
        // This method handles the snapping of the polarised ends of the magnet roads
        private void SnapFirstAndLastPoints_Inspector()
        {
            MagnetRoad thisMagnetRoad = _spline.gameObject.GetComponent<MagnetRoad>();
            var selected = _spline.GetControlPoint(_selectedIndex);
            var allRoads = FindObjectsOfType<MagnetRoad>();
            var allIntersections = FindObjectsOfType<Intersection>();
            if (selected == _spline.GetControlPoint(_spline.ControlPointCount - 1) || selected == _spline.GetControlPoint(0))
            {
                foreach (MagnetRoad road in allRoads)
                {
                    SnapPoint pointA = _spline.gameObject.GetComponent<MagnetRoad>().GetClosestSnapPointFromVector(_handleTransform.TransformPoint(selected));
                    SnapPoint pointB = road.GetClosestSnapPointFromVector(_handleTransform.TransformPoint(selected));
                    try
                    {
                        if (pointA.PointType != pointB.PointType)
                        {
                            if (road.gameObject != _spline.gameObject)
                            {
                                if (Vector3.Distance(_handleTransform.TransformPoint(selected), road.SnapNodeLeft.position) <= road.roadWidth / 3)
                                {
                                    _spline.SetControlPoint(_selectedIndex, _handleTransform.InverseTransformPoint(road.SnapNodeLeft.position));
                                    if (selected == _spline.GetControlPoint(_spline.ControlPointCount - 1))
                                    {
                                        float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(_spline.ControlPointCount - 2)));
                                        _spline.SetControlPoint(_selectedIndex - 1, _handleTransform.InverseTransformPoint(road.SnapNodeLeft.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    }
                                    if (selected == _spline.GetControlPoint(0))
                                    {
                                        float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(1)));
                                        _spline.SetControlPoint(1, _handleTransform.InverseTransformPoint(road.SnapNodeLeft.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    }
                                    if (thisMagnetRoad.IsEditableAtRuntime) thisMagnetRoad.GenerateRoadMesh(thisMagnetRoad.GenerateRoadVertexOutput(thisMagnetRoad.roadWidth));
                                }
                                if (Vector3.Distance(_handleTransform.TransformPoint(selected), road.SnapNodeRight.position) <= road.roadWidth / 3)
                                {
                                    _spline.SetControlPoint(_selectedIndex, _handleTransform.InverseTransformPoint(road.SnapNodeRight.position));
                                    if (selected == _spline.GetControlPoint(_spline.ControlPointCount - 1))
                                    {
                                        float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(_spline.ControlPointCount - 2)));
                                        _spline.SetControlPoint(_selectedIndex - 1, _handleTransform.InverseTransformPoint(road.SnapNodeRight.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    }
                                    if (selected == _spline.GetControlPoint(0))
                                    {
                                        float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(1)));
                                        _spline.SetControlPoint(1, _handleTransform.InverseTransformPoint(road.SnapNodeRight.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    }
                                    if (thisMagnetRoad.IsEditableAtRuntime) thisMagnetRoad.GenerateRoadMesh(thisMagnetRoad.GenerateRoadVertexOutput(thisMagnetRoad.roadWidth));
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
                }
                foreach (Intersection intersection in allIntersections)
                {
                    foreach (SnapPoint snapNode in intersection.SnapNodes)
                    {
                        if (Vector3.Distance(_handleTransform.TransformPoint(selected), snapNode.transform.position) < intersection.roadWidth / 3)
                        {
                            _spline.SetControlPoint(_selectedIndex, _handleTransform.InverseTransformPoint(snapNode.transform.position));
                            if (selected == _spline.GetControlPoint(_spline.ControlPointCount - 1))
                            {
                                float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(_spline.ControlPointCount - 2)));
                                _spline.SetControlPoint(_selectedIndex - 1, _handleTransform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                            }
                            if (selected == _spline.GetControlPoint(0))
                            {
                                float distance = Vector3.Distance(_handleTransform.TransformPoint(selected), _handleTransform.TransformPoint(_spline.GetControlPoint(1)));
                                _spline.SetControlPoint(1, _handleTransform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                            }
                            if (thisMagnetRoad.IsEditableAtRuntime) thisMagnetRoad.GenerateRoadMesh(thisMagnetRoad.GenerateRoadVertexOutput(thisMagnetRoad.roadWidth));
                        }
                    }
                }
            }
        }

        private void OnSceneGUI() 
        {
            _spline = target as BezierSpline;
            _handleTransform = _spline.transform;
            _handleRotation = Tools.pivotRotation == PivotRotation.Local ? _handleTransform.rotation : Quaternion.identity;

            var p0 = ShowPoint(0);
            for (int i = 1; i < _spline.ControlPointCount; i += 3)
            {
                var p1 = ShowPoint(i);
                var p2 = ShowPoint(i + 1);
                var p3 = ShowPoint(i + 2);

                Handles.color = Color.yellow;
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p2, p3);

                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
                p0 = p3;
            }
            ShowDirections();
        }

        private Vector3 ShowPoint(int index)
        {
            var point = _handleTransform.TransformPoint(_spline.GetControlPoint(index));
            var size = HandleUtility.GetHandleSize(point);
            Handles.color = modeColors[(int)_spline.GetControlPointMode(index)];
            if (Handles.Button(point, _handleRotation, size * _handleSize, size * _pickSize, Handles.DotHandleCap))
            {
                _selectedIndex = index;
                Repaint();
            }
            if (_selectedIndex == index)
            {
                EditorGUI.BeginChangeCheck();
                point = Handles.DoPositionHandle(point, _handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Move Point");
                    EditorUtility.SetDirty(_spline);
                    _spline.SetControlPoint(index, _handleTransform.InverseTransformPoint(point));
                }
            }
            return point;
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;
            var point = _spline.GetPoint(0f);
            Handles.DrawLine(point, point + _spline.GetDirection(0f) * _directionScale);
            var steps = _stepsPerCurve * _spline.CurveCount;
            for (int i = 1; i <= steps; i++)
            {
                point = _spline.GetPoint(i / (float)steps);
                Handles.DrawLine(point, point + _spline.GetDirection(i / (float)steps) * _directionScale);
            }
        }
    }
	#endif
}