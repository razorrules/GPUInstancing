using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Laio.GPUInstancing.Samples
{
    public class RotatingShapesSettings : MonoBehaviour
    {
        [SerializeField] private GPUInstancingRotatingShapes _gpuInstancing;

        [Header("Player Settings")]
        [SerializeField] private Slider _rotateSpeed;
        [SerializeField] private Slider _radiusSpeed;

        [SerializeField] private TextMeshProUGUI _rotateSpeedText;
        [SerializeField] private TextMeshProUGUI _radiusSpeedText;

        private void Start()
        {
            //Set slider to values from gpu instancing
            _rotateSpeed.value = _gpuInstancing.rotateSpeed;
            _radiusSpeed.value = _gpuInstancing.radiusSpeedBoost;

            // Add listener for on value change
            _rotateSpeed.onValueChanged.AddListener((x) => { _gpuInstancing.rotateSpeed = x; _rotateSpeedText.text = x.ToString("N1"); });
            _radiusSpeed.onValueChanged.AddListener((x) => { _gpuInstancing.radiusSpeedBoost = x; _radiusSpeedText.text = x.ToString("N1"); });

            // Update text
            _rotateSpeedText.text = _rotateSpeed.value.ToString("N1");
            _radiusSpeedText.text = _radiusSpeed.value.ToString("N1");

        }
    }
}