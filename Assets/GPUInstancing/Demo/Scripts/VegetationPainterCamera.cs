using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laio.GPUInstancing.Samples.VegetationPainter
{


    /// <summary>
    /// Super simple top down camera to navigate the vegetation painter scene.
    /// </summary>
    public class VegetationPainterCamera : MonoBehaviour
    {

        [Header("Zoom settings")]
        public float minZoom;
        public float maxZoom;
        public float zoomSpeed;
        [Header("Movement settings")]
        public float moveSpeed;

        private float _zoom;

        // Start is called before the first frame update
        void Start()
        {
            //To prevent jarring starts, set zoom to whatever our Y is
            _zoom = transform.position.y;
        }

        // Update is called once per frame
        void Update()
        {
            //Update zoom based on scroll
            _zoom = Mathf.Clamp(_zoom + Input.mouseScrollDelta.y * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            //Set the position based on movement and zoom
            transform.position = new Vector3(
                transform.position.x + Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime,
                _zoom,
                transform.position.z + Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
        }
    }
}