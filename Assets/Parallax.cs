using UnityEngine;

public class Parallax : MonoBehaviour
{
    public Transform cam;
    [Range(0f, 1f)] public float parallaxFactor = 0.5f;

    private Vector3 lastCamPos;

    void Start()
    {
        if (cam == null) cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0);
        lastCamPos = cam.position;
    }
}