using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Laio.GPUInstancing.Samples
{
    public class PillarSettings : MonoBehaviour
    {
        [SerializeField] private GPUInstancingPillars _gpuInstancing;

        [Header("Player Settings")]
        [SerializeField] private Slider _playerMinDist;
        [SerializeField] private Slider _playerMaxDist;
        [SerializeField] private Slider _playerHeight;
        [Space]
        [SerializeField] private TextMeshProUGUI _playerMinDistText;
        [SerializeField] private TextMeshProUGUI _playerMaxDistText;
        [SerializeField] private TextMeshProUGUI _playerHeightText;

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
            _playerMinDist.value = _gpuInstancing.minDist;
            _playerMaxDist.value = _gpuInstancing.maxDist;
            _playerHeight.value = _gpuInstancing.height;

            _scale.value = _gpuInstancing.scale;
            _heightScale.value = _gpuInstancing.heightScale;
            _timeScale.value = _gpuInstancing.timeScale;

            // Add listener for on value change
            _playerMinDist.onValueChanged.AddListener((x) => { _gpuInstancing.minDist = x; _playerMinDistText.text = x.ToString("N1"); });
            _playerMaxDist.onValueChanged.AddListener((x) => { _gpuInstancing.maxDist = x; _playerMaxDistText.text = x.ToString("N1"); });
            _playerHeight.onValueChanged.AddListener((x) => { _gpuInstancing.height = x; _playerHeightText.text = x.ToString("N1"); });

            _scale.onValueChanged.AddListener((x) => { _gpuInstancing.scale = x; _scaleText.text = x.ToString("N1"); });
            _heightScale.onValueChanged.AddListener((x) => { _gpuInstancing.heightScale = x; _heightScaleText.text = x.ToString("N1"); });
            _timeScale.onValueChanged.AddListener((x) => { _gpuInstancing.timeScale = x; _timeScaleText.text = x.ToString("N1"); });

            // Update text
            _playerMinDistText.text = _playerMinDist.value.ToString("N1");
            _playerMaxDistText.text = _playerMaxDist.value.ToString("N1");
            _playerHeightText.text = _playerHeight.value.ToString("N1");

            _scaleText.text = _scale.value.ToString("N1");
            _heightScaleText.text = _heightScale.value.ToString("N1");
            _timeScaleText.text = _timeScale.value.ToString("N1");
        }
    }
}