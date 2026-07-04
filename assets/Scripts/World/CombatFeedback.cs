using UnityEngine;

// Listens to GameEvents.CombatHit and spawns floating damage numbers + a small particle
// burst + an optional hit sound. hitSound is left unassigned by default — no audio asset
// is checked into the project yet.
public class CombatFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private Font damageFont;

    void OnEnable()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CombatHit += OnCombatHit;
        }
    }

    void OnDisable()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CombatHit -= OnCombatHit;
        }
    }

    void OnCombatHit(Vector2 worldPosition, int amount, bool isCrit)
    {
        SpawnDamageNumber(worldPosition, amount, isCrit);
        SpawnHitParticles(worldPosition, isCrit);
        PlayHitSound(worldPosition, isCrit);
    }

    void SpawnDamageNumber(Vector2 worldPosition, int amount, bool isCrit)
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.position = worldPosition + new Vector2(-0.12f, 0.28f);

        TextMesh text = go.AddComponent<TextMesh>();
        text.text = amount.ToString();
        text.characterSize = 0.12f;
        text.fontSize = 48;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = isCrit ? new Color(1f, 0.95f, 0.42f) : Color.white;
        if (damageFont != null) text.font = damageFont;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 120;

        go.transform.localScale = Vector3.one * (isCrit ? 1.3f : 1.0f);

        StartCoroutine(AnimateDamageNumber(go));
    }

    System.Collections.IEnumerator AnimateDamageNumber(GameObject go)
    {
        float duration = 0.45f;
        float elapsed = 0f;
        Vector3 start = go.transform.position;
        Vector3 end = start + new Vector3(0f, 0.36f, 0f);
        TextMesh text = go.GetComponent<TextMesh>();
        Color startColor = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            go.transform.position = Vector3.Lerp(start, end, t);
            text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        Destroy(go);
    }

    void SpawnHitParticles(Vector2 worldPosition, bool isCrit)
    {
        GameObject go = new GameObject("HitParticles");
        go.transform.position = worldPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.22f;
        main.loop = false;
        main.startLifetime = 0.22f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.36f, 1.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(isCrit ? 1.4f : 0.8f, isCrit ? 2.1f : 1.4f);
        main.startColor = isCrit ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.95f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, isCrit ? 14 : 9) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60f;

        ps.Play();
        Destroy(go, 0.6f);
    }

    void PlayHitSound(Vector2 worldPosition, bool isCrit)
    {
        if (audioSource == null || hitSound == null) return;
        audioSource.transform.position = worldPosition;
        audioSource.pitch = isCrit ? 1.18f : 0.96f;
        audioSource.PlayOneShot(hitSound);
    }
}
