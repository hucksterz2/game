using UnityEngine;
using UnityEngine.InputSystem;

public class GrapplingHook : MonoBehaviour
{
    [Header("Настройки крюка")]
    public float maxDistance = 10f;
    public float pullSpeed = 8f;
    public LayerMask grappleLayer;

    private LineRenderer line;
    private DistanceJoint2D joint;
    private Vector2 grapplePoint;
    private bool isGrappling = false;
    private Rigidbody2D rb;
    private ParticleSystem hitParticles;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();

        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        line.enabled = false;

        GameObject psObj = new GameObject("GrappleHitParticles");
        hitParticles = psObj.AddComponent<ParticleSystem>();
        hitParticles.Stop();

        var main = hitParticles.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 4f;
        main.startSize = 0.07f;
        main.maxParticles = 20;
        main.playOnAwake = false;

        var emission = hitParticles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
            { new ParticleSystem.Burst(0f, 12) });

        var shape = hitParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryGrapple();

        if (Mouse.current.rightButton.wasPressedThisFrame && isGrappling)
            StopGrapple();

        if (isGrappling)
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, grapplePoint);

            if (joint != null)
                joint.distance = Mathf.Max(0.5f,
                    joint.distance - pullSpeed * Time.deltaTime);
        }
    }

    void TryGrapple()
    {
        if (isGrappling) { StopGrapple(); return; }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());
        Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, dir, maxDistance, grappleLayer);

        if (hit.collider != null)
        {
            grapplePoint = hit.point;
            isGrappling = true;

            joint = gameObject.AddComponent<DistanceJoint2D>();
            joint.autoConfigureDistance = false;
            joint.enableCollision = true;
            joint.connectedAnchor = grapplePoint;
            joint.distance = Vector2.Distance(transform.position, grapplePoint);

            line.enabled = true;

            SpawnHitParticles(hit);
        }
    }

    void SpawnHitParticles(RaycastHit2D hit)
    {
        Color surfaceColor = new Color(0.4f, 0.25f, 0.1f);

        SpriteRenderer sr = hit.collider.GetComponent<SpriteRenderer>();
        if (sr != null) surfaceColor = sr.color;

        UnityEngine.Tilemaps.Tilemap tm =
            hit.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tm != null)
        {
            var tilePos = tm.WorldToCell(hit.point - hit.normal * 0.01f);
            UnityEngine.Tilemaps.TileBase tile = tm.GetTile(tilePos);
            if (tile != null)
            {
                surfaceColor = new Color(0.35f, 0.2f, 0.08f);
            }
        }

        var main = hitParticles.main;
        main.startColor = surfaceColor;
        main.startLifetime = 0.5f;

        hitParticles.transform.position = grapplePoint;
        hitParticles.Play();

        Invoke(nameof(StopParticles), 0.6f);
    }

    void StopParticles()
    {
        hitParticles.Stop();
        hitParticles.Clear();
    }

    void StopGrapple()
    {
        isGrappling = false;
        line.enabled = false;
        if (joint != null) Destroy(joint);
    }
}