using UnityEngine;

/*
 * QuadraticBezierCurve owns the three control-point Transforms in the Demo
 * scene. This is where their world positions connect to quadratic math,
 * Scene-view gizmos, and the Play Mode line.
 */

public class QuadraticBezierCurve : MonoBehaviour
{
    [Header("Bezier Points")]
    public Transform p0;
    public Transform p1;
    public Transform p2;

    public int numSamples = 10;

    void Update()
    {
        LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.positionCount = numSamples;
        
        for (int i = 0; i < numSamples; i++)
        {
            // Debug.Log("Index: " + i + " " + "Point: " + SamplePoint((float) i / 10));
            float t = (float) i / (numSamples - 1);
            lineRenderer.SetPosition(i, SamplePoint(t));
            // position x = -22.53 and z = -1.56 is the beginning of the curve, left
            // position x = -10.45 and z = 4.67 is the middle of the curve
            // position x = 0.6 and z = -1.73 is the end of the curve, right
        }
    }

    void OnDrawGizmos()
    {
        if (p0 == null || p1 == null || p2 == null) return;
        CurveGizmos.Draw(numSamples, SamplePoint, p0, p1, p2);
    }

    public Vector3 SamplePoint(float t)
    {
        Vector3 sample = QuadraticBezierMath.SamplePointDeCasteljau(p0.position, p1.position, p2.position, t);
        
        return sample;
    }

    public Vector3 SampleTangent(float t)
    {
        return QuadraticBezierMath.SampleTangentBernstein(p0.position, p1.position, p2.position, t);
    }
}
