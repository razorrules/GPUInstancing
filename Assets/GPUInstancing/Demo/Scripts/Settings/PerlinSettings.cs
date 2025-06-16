using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Laio.GPUInstancing.Samples
{
    public class PerlinSettings : MonoBehaviour
    {
        [SerializeField] private GPUInstancingPerlinNoise _gpuInstancing;

        [Header("Player Settings")]
        [SerializeField] private Slider _yScale;

        [SerializeField] private TextMeshProUGUI _yScaleText;

        [Header("Pillar Settings")]
        [SerializeField] private Slider _scale;
        [SerializeField] private Slider _heightScale;
        [SerializeField] private Slider _timeScale;
        [Space]
        [SerializeField] private TextMeshProUGUI _scaleText;
        [SerializeField] private TextMeshProUGUI _heightScaleText;
        [SerializeField] private TextMeshProUGUI _timeScaleText;

        private void Awake()
        {
            //Set slider to values from gpu instancing
            _yScale.value = _gpuInstancing.meshYScale;

            _scale.value = _gpuInstancing.scale;
            _heightScale.value = _gpuInstancing.heightScale;
            _timeScale.value = _gpuInstancing.timeScale;

            // Add listener for on value change
            _yScale.onValueChanged.AddListener((x) => { _gpuInstancing.meshYScale = x; _yScaleText.text = x.ToString("N1"); });

            _scale.onValueChanged.AddListener((x) => { _gpuInstancing.scale = x; _scaleText.text = x.ToString("N1"); });
            _heightScale.onValueChanged.AddListener((x) => { _gpuInstancing.heightScale = x; _heightScaleText.text = x.ToString("N1"); });
            _timeScale.onValueChanged.AddListener((x) => { _gpuInstancing.timeScale = x; _timeScaleText.text = x.ToString("N1"); });

            // Update text
            _yScaleText.text = _yScale.value.ToString("N1");

            _scaleText.text = _scale.value.ToString("N1");
            _heightScaleText.text = _heightScale.value.ToString("N1");
            _timeScaleText.text = _timeScale.value.ToString("N1");
        }
    }
}