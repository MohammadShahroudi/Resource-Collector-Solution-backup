using UnityEngine;

/*
 * FollowCurve tracks time for a trip along a QuadraticBezierCurve. Its Update
 * loop is where movement and facing belong; triggerReset starts the trip again.
 */

public class FollowCurve : MonoBehaviour
{
    public QuadraticBezierCurve curve;
    public float duration = 3f;
    public bool triggerReset;

    float _elapsed;

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);
        
        transform.position = curve.SamplePoint(t);
        transform.forward = curve.SampleTangent(t);

        if (triggerReset)
        {
            triggerReset = false;
            _elapsed = 0;
        }
    }
}
