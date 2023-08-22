using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Brush : MonoBehaviour
{
    public enum BrushType
    {
        Tree,
        Brush,
        Grass
    }
    public Transform brushObject;

    private Camera cam;

    public float brushSize = 0;
    public float density = 0;

    [SerializeField] private VegetationPainter _treePainter;
    [SerializeField] private VegetationPainter _grassPainter;
    [SerializeField] private VegetationPainter _bushPainter;

    private VegetationPainter Painter;

    private BrushType _brushType;

    // Start is called before the first frame update
    void Start()
    {
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

        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
        {
            brushObject.transform.position = hit.point + Vector3.up * .1f;
            //We use a plane, so just divide by two
            brushObject.transform.localScale = Vector3.one * (brushSize / 4.0f);
            position = hit.point;
        }
        else
            return;

        if (EventSystem.current.IsPointerOverGameObject())
            return;
        //Add
        if (Input.GetMouseButton(0))
        {
            List<Vector3> generatedPoints = new List<Vector3>();
            for (int i = 0; i < density; i++)
            {
                generatedPoints.Add(
                    position +
                    new Vector3(UnityEngine.Random.Range(-brushSize, brushSize),
                    0,
                    UnityEngine.Random.Range(-brushSize, brushSize)));
            }
            Painter.AddPoints(generatedPoints);
        }

        //Remove
        if (Input.GetMouseButton(1))
        {
            Painter.RemovePoints(position, brushSize);
        }

    }


    public void SetBrushType(BrushType brushType)
    {
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
