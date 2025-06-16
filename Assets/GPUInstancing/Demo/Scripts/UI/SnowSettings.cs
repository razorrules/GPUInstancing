using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Laio.GPUInstancing.Samples
{
    public class SnowSettings : MonoBehaviour
    {
        [SerializeField] private GPUInstancingSnowflakes _gpuInstancing;

        [Header("Settings")]
        [SerializeField] private Toggle _toggleWindow;

        [SerializeField] private Slider _windX;
        [SerializeField] private Slider _windY;
        [SerializeField] private Slider _windZ;
        [Space]
        [SerializeField] private TextMeshProUGUI _windXText;
        [SerializeField] private TextMeshProUGUI _windYText;
        [SerializeField] private TextMeshProUGUI _windZText;

        private void Awake()
        {
            _toggleWindow.isOn = !_gpuInstancing.pauseMovement;
            _toggleWindow.onValueChanged.AddListener((x) => { _gpuInstancing.pauseMovement = !x; });


            _windX.value = _gpuInstancing.movement.x;
            _windY.value = _gpuInstancing.movement.y;
            _windZ.value = _gpuInstancing.movement.z;

            _windX.onValueChanged.AddListener((x) => { _gpuInstancing.movement.x = x; _windXText.text = x.ToString("N1"); });
            _windY.onValueChanged.AddListener((y) => { _gpuInstancing.movement.y = y; _windYText.text = y.ToString("N1"); });
            _windZ.onValueChanged.AddListener((z) => { _gpuInstancing.movement.z = z; _windZText.text = z.ToString("N1"); });

            _windXText.text = _windX.value.ToString("N1");
            _windYText.text = _windY.value.ToString("N1");
            _windZText.text = _windZ.value.ToString("N1");
        }
    }
}