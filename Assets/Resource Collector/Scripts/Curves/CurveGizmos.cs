using System;
using UnityEngine;

/*
 * CurveGizmos is the shared Scene-view helper for quadratic and cubic curves.
 * Both curve components pass in their control points and a way to sample the
 * curve. This class is responsible for drawing the markers, control polygon,
 * and curve.
 */

public static class CurveGizmos
{
    public static void Draw(int numSamples, Func<float, Vector3> samplePoint, params Transform[] controlPoints)
    {
        // TODO Slice 1.1: mark each control point with a white wire sphere of radius 0.1.
        // The cubic reuses this helper, so do not assume three points.
        // Next: Slice 1.2 in QuadraticBezierCurve.cs, where you can see the markers.
        Gizmos.color = Color.white;
        foreach (Transform t in controlPoints)
            Gizmos.DrawWireSphere(t.position, 0.1f);
        
        // TODO Slice 1.3: connect adjacent control points with red lines. Leave it open.
        // Check: the polygon follows moved points, even before any curve math works.
        // Next: Slice 1.4 in Bezier/QuadraticBezierMath.cs.
        Gizmos.color = Color.red;
        Vector3 lastPosition = controlPoints[0].position;
        for (int i = 1; i < controlPoints.Length; i++)
        {
            Gizmos.DrawLine(lastPosition, controlPoints[i].position);
            lastPosition = controlPoints[i].position;
        }
        
        // TODO Slice 1.6: draw the curve in white with numSamples points from samplePoint.
        // Space them evenly in t and include both endpoints.
        // Check: the curve reaches both endpoints and follows moved points.
        // Next: Slice 1.7 in QuadraticBezierCurve.cs, Start.
        Gizmos.color = Color.white;
        Vector3 lastSample = controlPoints[0].position;
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / (numSamples - 1);
            Vector3 position = samplePoint(t);
            Gizmos.DrawLine(lastSample, position);
            lastSample = position;
        }
        
    }
}
