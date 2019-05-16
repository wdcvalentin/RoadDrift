// ************************************************************************
// Majority of bezier & spline code is open source and availiable from: 
// http://catlikecoding.com/unity/tutorials/curves-and-splines/
// ************************************************************************

using UnityEngine;

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
    public static class Bezier
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }
    }


	#if UNITY_EDITOR
    /// <summary>
    /// (Bezier Spline)
    /// </summary>
    [AddComponentMenu("")]
    public class BezierCurve : MonoBehaviour
    {

        public Vector3[] points;

        public void Reset()
        {
            points = new Vector3[] {
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(3f, 0f, 0f),
            new Vector3(4f, 0f, 0f)
        };
        }

        public Vector3 GetPoint(float t)
        {
            return transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }
    }


    /// <summary>
    /// (Bezier Spline)
    /// </summary>
    [CustomEditor(typeof(BezierCurve))]
    public class BezierCurveEditorInspector : Editor
    {
        private BezierCurve _curve;
        private Transform _handleTransform;
        private Quaternion _handleRotation;

        private const int _lineSteps = 10;

        private const float _directionScale = 0.5f;

        private void OnSceneGUI()
        {
            _curve = target as BezierCurve;
            _handleTransform = _curve.transform;
            _handleRotation = Tools.pivotRotation == PivotRotation.Local ? _handleTransform.rotation : Quaternion.identity;

            var p0 = ShowPoint(0);
            var p1 = ShowPoint(1);
            var p2 = ShowPoint(2);
            var p3 = ShowPoint(3);

            Handles.color = Color.grey;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            ShowDirections();
            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
        }

        private Vector3 ShowPoint(int index)
        {
            var point = _handleTransform.TransformPoint(_curve.points[index]);
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, _handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_curve, "Move Point");
                EditorUtility.SetDirty(_curve);
                _curve.points[index] = _handleTransform.InverseTransformPoint(point);
            }
            return point;
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;
            var point = _curve.GetPoint(0f);
            Handles.DrawLine(point, point + _curve.GetDirection(0f) * _directionScale);
            for (int i = 1; i <= _lineSteps; i++)
            {
                point = _curve.GetPoint(i / (float)_lineSteps);
                Handles.DrawLine(point, point + _curve.GetDirection(i / (float)_lineSteps) * _directionScale);
            }
        }
    }
	#endif
}