using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laio.GPUInstancing.Samples.VegetationPainter
{

    /// <summary>
    /// Handles the UI for the Brush
    /// </summary>
    public class VegetationPainterUI : MonoBehaviour
    {

        [Header("Sliders")]
        [SerializeField] private Slider _brushSize;
        [SerializeField] private Slider _density;

        [SerializeField] private TextMeshProUGUI _brushSizeText;
        [SerializeField] private TextMeshProUGUI _densityText;

        [Header("Buttons")]
        [SerializeField] private Button treeButton;
        [SerializeField] private Button brushButton;
        [SerializeField] private Button grassButton;

        private Brush _brush;

        // Start is called before the first frame update
        void Start()
        {
            //Find our brush. If there is none, throw error and disable object
            _brush = FindObjectOfType<Brush>();
            if (_brush == null)
            {
                Debug.LogError("No brush in scene to reference.");
                gameObject.SetActive(false);
            }

            _brushSize.onValueChanged.AddListener((x) => { UpdateSize(x); _brushSizeText.text = x.ToString("N1"); });
            _density.onValueChanged.AddListener((x) => { UpdateDensity(x); _densityText.text = x.ToString("N1"); });

            //Default the values on sliders to what is currently being used
            _brushSize.value = _brush.brushSize;
            _density.value = _brush.density;

            //Add listeners for all the buttons
            treeButton.onClick.AddListener(() => SelectBrush(Brush.BrushType.Tree));
            brushButton.onClick.AddListener(() => SelectBrush(Brush.BrushType.Brush));
            grassButton.onClick.AddListener(() => SelectBrush(Brush.BrushType.Grass));

            SelectBrush(Brush.BrushType.Tree);
        }

        public void SelectBrush(Brush.BrushType brushType)
        {
            _brush.SetBrushType(brushType);

            treeButton.targetGraphic.color = Color.white;
            brushButton.targetGraphic.color = Color.white;
            grassButton.targetGraphic.color = Color.white;

            Color selectedColor = new Color(.6f, 1.0f, .6f);

            switch (brushType)
            {
                case Brush.BrushType.Tree:
                    treeButton.targetGraphic.color = selectedColor;
                    break;
                case Brush.BrushType.Brush:
                    brushButton.targetGraphic.color = selectedColor;
                    break;
                case Brush.BrushType.Grass:
                    grassButton.targetGraphic.color = selectedColor;
                    break;
            }
        }

        /// <summary>
        /// Called from slider, updates the brush size
        /// </summary>
        /// <param name="size"></param>
        public void UpdateSize(float size)
        {
            _brush.brushSize = size;
        }

        /// <summary>
        /// Called from the slider, updates the density
        /// </summary>
        /// <param name="density"></param>
        public void UpdateDensity(float density)
        {
            _brush.density = density;
        }


    }
}