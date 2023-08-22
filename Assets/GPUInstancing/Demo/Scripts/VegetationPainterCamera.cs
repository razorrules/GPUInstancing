using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationPainterCamera : MonoBehaviour
{
    public float minZoom;
    public float maxZoom;
    public float zoomSpeed;
    [Space(5)]
    public float moveSpeed;

    private float _zoom;

    // Start is called before the first frame update
    void Start()
    {
        _zoom = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        _zoom = Mathf.Clamp(_zoom + Input.mouseScrollDelta.y * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
        transform.position = new Vector3(
            transform.position.x + Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime,
            _zoom,
            transform.position.z + Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
    }
}
