using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Laio.GPUInstancing.Samples
{
    public class LODSettings : MonoBehaviour
    {
        [SerializeField] private LODInstanceManager _gpuInstancing;

        [Header("Player Settings")]
        [SerializeField] private Slider _lod1;
        [SerializeField] private Slider _lod2;

        [SerializeField] private TextMeshProUGUI _lod1Text;
        [SerializeField] private TextMeshProUGUI _lod2Text;

        private void Start()
        {
            //Set slider to values from gpu instancing
            _lod1.value = _gpuInstancing.GetLODDistance(0);
            _lod2.value = _gpuInstancing.GetLODDistance(1);

            // Add listener for on value change
            _lod1.onValueChanged.AddListener((x) => { _gpuInstancing.SetLODDistance(0, x); _lod1Text.text = x.ToString("N1"); });
            _lod2.onValueChanged.AddListener((x) => { _gpuInstancing.SetLODDistance(1, x); _lod2Text.text = x.ToString("N1"); });

            // Update text
            _lod1Text.text = _lod1.value.ToString("N1");
            _lod2Text.text = _lod2.value.ToString("N1");

        }
    }
}