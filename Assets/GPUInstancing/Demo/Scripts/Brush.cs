using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GPUInstancing.Samples.VegetationPainter
{

    /// <summary>
    /// Handles the brush for painting the trees. Contains a reference to different vegetation
    /// painters to pass point data to. VegetationPainterUI handles brush size, brush type, etc.
    /// </summary>
    public class Brush : MonoBehaviour
    {
        public enum BrushType
        {
            Tree,
            Brush,
            Grass
        }

        public Transform brushObject;
        public float brushSize = 0;
        public float density = 0;

        [Space]
        [SerializeField] private VegetationPainter _treePainter;
        [SerializeField] private VegetationPainter _grassPainter;
        [SerializeField] private VegetationPainter _bushPainter;

        /// <summary>
        /// Currently selected painter
        /// </summary>
        private VegetationPainter Painter;
        private Camera cam;
        private BrushType _brushType;

        // Start is called before the first frame update
        void Start()
        {
            //Quickly error check if any of the vegetation painters are null
            if (_treePainter == null || _grassPainter == null || _bushPainter == null)
            {
                Debug.LogError("Vegetation painters are set to null.");
                gameObject.SetActive(false);
            }

            cam = Camera.main;
            SetBrushType(BrushType.Tree);
        }

        // Update is called once per frame
        void Update()
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            Vector3 position;

            //Quickly raycast and get the position
            if (Physics.Raycast(ray, out RaycastHit hit, 1000))
            {
                brushObject.transform.position = hit.point + Vector3.up * .1f;
                //We use a plane, so just divide by two
                brushObject.transform.localScale = Vector3.one * (brushSize / 4.0f);
                position = hit.point;
            }
            else //Return if failed raycast
                return;

            //Return if you are hovering over UI objects, as we don't want to paint while you click a button
            if (EventSystem.current.IsPointerOverGameObject())
                return;


            //Add
            if (Input.GetMouseButton(0))
            {
                //Generate random points based on density in range
                //TODO: This is generated as a square, normalize it so it will match brush size
                List<Vector3> generatedPoints = new List<Vector3>();
                Vector2 random;
                for (int i = 0; i < density; i++)
                {
                    random = Random.insideUnitCircle;
                    generatedPoints.Add(new Vector3(
                        position.x + random.x * brushSize,
                        0,
                        position.z + random.y * brushSize));
                }

                Painter.AddPoints(generatedPoints);
            }

            //Remove points 
            if (Input.GetMouseButton(1))
            {
                Debug.Log("Removing");
                Painter.RemovePoints(position, brushSize);
            }

        }

        /// <summary>
        /// Update the brush type
        /// </summary>
        /// <param name="brushType"></param>
        public void SetBrushType(BrushType brushType)
        {
            //Update brushType and set the Painter
            _brushType = brushType;
            switch (brushType)
            {
                case BrushType.Brush:
                    Painter = _bushPainter;
                    break;
                case BrushType.Tree:
                    Painter = _treePainter;
                    break;
                case BrushType.Grass:
                    Painter = _grassPainter;
                    break;
                default:
                    break;
            }

        }

    }
}