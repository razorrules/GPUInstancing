using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laio.GPUInstancing;

public class CameraToggle : MonoBehaviour
{

    [SerializeField] private Camera _freefloatCamera;
    [SerializeField] private Camera _rotateCamera;

    private bool _isFreeCam;
    private InstanceManagerBase[] _gpuInstancing;

    // Start is called before the first frame update
    void Start()
    {
        _freefloatCamera.gameObject.SetActive(true);
        _rotateCamera.gameObject.SetActive(false);
        _isFreeCam = true;

        _gpuInstancing = GameObject.FindObjectsByType<InstanceManagerBase>(FindObjectsSortMode.None);
        Debug.Log("Found: " + _gpuInstancing.Length);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_isFreeCam)
            {
                _freefloatCamera.gameObject.SetActive(false);
                _rotateCamera.gameObject.SetActive(true);
                foreach (var instancer in _gpuInstancing)
                    instancer.SetCamera(_rotateCamera);
            }
            else
            {
                _freefloatCamera.gameObject.SetActive(true);
                _rotateCamera.gameObject.SetActive(false);
                foreach (var instancer in _gpuInstancing)
                    instancer.SetCamera(_freefloatCamera);
            }

            _isFreeCam = !_isFreeCam;
        }
    }
}
