using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationPainterUI : MonoBehaviour
{

    public Slider _bushSize;
    public Slider _density;

    private Brush _brush;

    public Button treeButton;
    public Button brushButton;
    public Button grassButton;
    // Start is called before the first frame update
    void Start()
    {
        _brush = FindObjectOfType<Brush>();
        if (_brush == null)
        {
            Debug.LogError("No brush in scene to reference.");
            gameObject.SetActive(false);
        }

        _bushSize.value = _brush.brushSize;
        _density.value = _brush.density;

        treeButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Tree));
        brushButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Brush));
        grassButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Grass));
    }

    public void UpdateSize(float size)
    {
        _brush.brushSize = size;
    }

    public void UpdateDensity(float density)
    {
        _brush.density = density;
    }


}
