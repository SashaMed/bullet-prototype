using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    public GameObject visuals;
    public float shakeAmplitude = 1;

    public float Radius => visuals.transform.localScale.x * 0.5f;

    public void SetInitialSize(float radius)
    {
        DisableShake();
        StartCoroutine(InitialSizeCoroutine (radius));
    }

    private IEnumerator InitialSizeCoroutine(float radius)
    {
        var t = 0f;
        var time = .4f;
        while (t < time) 
        {
            var r = Mathf.Lerp(0, radius, t / time);
            visuals.transform.localScale = new Vector3(r, r, r);
            t += Time.deltaTime;
            yield return null;
        }
        visuals.transform.localScale = new Vector3(radius, radius, radius);

    }

    private bool shake;

    public void DisableShake()
    {
        shake = false;
    }

    public void Resize(float diameter)
    {
        StopAllCoroutines();
        visuals.transform.localScale = new Vector3(diameter, diameter, diameter);

        shake = true;
    }

    private void Update()
    {
        visuals.transform.localPosition = shake ? Random.insideUnitSphere * shakeAmplitude : Vector3.zero;
    }

}
