using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Color infectedColor = new Color(1f, 0.6f, 0.15f);
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private float maxHp = 1f;
    [SerializeField] private bool conducts = true;
    [SerializeField] private float colorDuration = 0.2f;
    [SerializeField] private float explodeDuration = 0.35f;

    private float hp;
    private Color initialColor;
    private Coroutine colorRoutine;

    public Vector3 Position => transform.position;
    public float Radius => radius;
    public bool Conducts => conducts;
    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        initialColor = bodyRenderer.sharedMaterial.color;
    }

    public void Configure(float obstacleHp, bool conductsWave)
    {
        maxHp = obstacleHp;
        hp = obstacleHp;
        conducts = conductsWave;
    }

    public bool Hit(float energy, float delay)
    {
        if (!IsAlive || energy < 0f)
            return false;

        if (energy >= hp)
        {
            hp = 0f;
            IsAlive = false;
            if (colorRoutine != null)
                StopCoroutine(colorRoutine);
            StartCoroutine(ExplodeRoutine(delay));
            return true;
        }

        hp -= energy;
        if (colorRoutine != null)
            StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(ColorRoutine(delay, Color.Lerp(initialColor, infectedColor, 1f - hp / maxHp)));
        return false;
    }

    private IEnumerator ColorRoutine(float delay, Color target)
    {
        yield return new WaitForSeconds(delay);

        var material = bodyRenderer.material;
        Color from = material.color;
        for (float t = 0f; t < 1f; t += Time.deltaTime / colorDuration)
        {
            material.color = Color.Lerp(from, target, t);
            yield return null;
        }
        material.color = target;
    }

    private IEnumerator ExplodeRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        bodyRenderer.material.color = infectedColor;

        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / explodeDuration)
        {
            float scale = t < 0.3f ? Mathf.Lerp(1f, 1.4f, t / 0.3f) : Mathf.Lerp(1.4f, 0f, (t - 0.3f) / 0.7f);
            transform.localScale = startScale * scale;
            yield return null;
        }

        Destroy(gameObject);
    }
}
