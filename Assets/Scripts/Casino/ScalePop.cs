using System.Collections;
using UnityEngine;

// Pops a transform in (grows with a small overshoot) or out (shrinks, easing in). Used by the roulette wheels.
public static class ScalePop
{
    public static IEnumerator Run(Transform target, float from, float to, float duration)
    {
        if (target == null)
            yield break;
        bool growing = to > from;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            float eased = growing ? 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f) : k * k;
            target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, eased);
            yield return null;
        }
        target.localScale = Vector3.one * to;
    }
}
