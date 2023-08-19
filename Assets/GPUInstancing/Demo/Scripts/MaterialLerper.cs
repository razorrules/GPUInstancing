using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancing.Samples
{

    public class MaterialLerper : MonoBehaviour
    {
        public Color[] colors;

        public float timeToLerp;

        [Space(10)]
        public Material mat;

        private float _curTime;
        private int _curIndex;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            _curTime += Time.deltaTime / timeToLerp;
            if (_curTime > 1)
            {
                _curTime = 0;
                if (_curIndex >= colors.Length - 1)
                    _curIndex = 0;
                else
                    _curIndex++;
            }
            mat.SetColor("_EmissionColor", GetColorLerp());
        }

        private Color GetColorLerp()
        {
            if (colors == null || colors.Length == 0)
                return Color.red;
            if (colors.Length < 2)
                return colors[0];

            if (_curIndex == colors.Length - 1)
                return Color.Lerp(colors[colors.Length - 1], colors[0], _curTime);
            return Color.Lerp(colors[_curIndex], colors[_curIndex + 1], _curTime);

        }

    }
}