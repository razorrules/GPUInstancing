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
        public Slider _bushSize;
        public Slider _density;
        [Header("Buttons")]
        public Button treeButton;
        public Button brushButton;
        public Button grassButton;

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

            //Default the values on sliders to what is currently being used
            _bushSize.value = _brush.brushSize;
            _density.value = _brush.density;

            //Add listeners for all the buttons
            treeButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Tree));
            brushButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Brush));
            grassButton.onClick.AddListener(() => _brush.SetBrushType(Brush.BrushType.Grass));
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