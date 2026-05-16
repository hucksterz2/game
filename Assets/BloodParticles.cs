using UnityEngine;

public class BloodParticles : MonoBehaviour
{
    public static void Spawn(Vector3 worldPos, int damage = 10, Transform parent = null)
    {
        var go = new GameObject("BloodFX");

        if (parent != null)
        {
            go.transform.SetParent(parent, worldPositionStays: true);
        }

        go.transform.position = worldPos;
        go.AddComponent<BloodParticles>().Play(damage, parent != null);
    }

    void Play(int damage, bool followParent)
    {
        var ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake     = false;
        main.simulationSpace = followParent
            ? ParticleSystemSimulationSpace.Local
            : ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.28f, 0.58f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.gravityModifier = 0.75f;
        main.maxParticles    = 40;

        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.95f, 0.05f, 0.05f), 0.00f),
                new GradientColorKey(new Color(0.60f, 0.01f, 0.01f), 0.55f),
                new GradientColorKey(new Color(0.25f, 0.00f, 0.00f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        int count = Mathf.Clamp(6 + damage / 3, 6, 22);
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.07f;
        shape.rotation  = new Vector3(-90f, 0f, 0f);

        var col  = ps.colorOverLifetime;
        col.enabled = true;
        var fg = new Gradient();
        fg.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(fg);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material     = new Material(Shader.Find("Sprites/Default"));
        rend.sortingOrder = 15;

        ps.Play();
        Destroy(gameObject, 2f);
    }
}
