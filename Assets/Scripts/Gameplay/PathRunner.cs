using System;
using System.Collections;
using UnityEngine;

public class PathRunner : MonoBehaviour
{
    [SerializeField] private float startDelay = 0.8f;
    [SerializeField] private float hopLength = 1.5f;
    [SerializeField] private float hopHeight = 0.6f;
    [SerializeField] private float hopDuration = 0.35f;

    public void Run(Transform ball, Vector3 target, Action onArrived)
    {
        StopAllCoroutines();
        StartCoroutine(RunRoutine(ball, target, onArrived));
    }

    public void Stop()
    {
        StopAllCoroutines();
    }

    private IEnumerator RunRoutine(Transform ball, Vector3 target, Action onArrived)
    {
        yield return new WaitForSeconds(startDelay);

        Vector3 start = ball.position;
        Vector3 end = new Vector3(target.x, start.y, target.z);
        int hops = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / hopLength));

        Vector3 step = (end - start) / hops;

        for (int i = 0; ; i++)
        {
            if (i == hops)
                onArrived?.Invoke();

            Vector3 from = start + step * i;
            Vector3 to = from + step;
            for (float t = 0f; t < 1f; t += Time.deltaTime / hopDuration)
            {
                ball.position = Vector3.Lerp(from, to, t) + Vector3.up * (4f * hopHeight * t * (1f - t));
                yield return null;
            }
            ball.position = to;
        }
    }
}
