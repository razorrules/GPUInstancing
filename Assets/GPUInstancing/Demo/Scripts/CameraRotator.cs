using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{

    [SerializeField] private float _rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles += Vector3.up * _rotationSpeed * Time.deltaTime;
    }
}
