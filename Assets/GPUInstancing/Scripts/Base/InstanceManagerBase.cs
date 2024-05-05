using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Diagnostics;
#endif
using UnityEngine;

namespace Laio.GPUInstancing
{

    /// <summary>
    /// Base class for all instance managers. This is simply a template of methods that also contains
    /// the most basic data and references. Such as Camera and number of instances. 
    /// Performance is tracked here and is wrapped in UNITY_EDITOR
    /// </summary>
    public abstract class InstanceManagerBase : MonoBehaviour
    {

        //Constants
        public const KeyCode DISPLAY_PERFORMANCE_HOTKEY = KeyCode.Equals;
        public const bool DEFAULT_PERFORMANCE_ON = true;

        [Header("Base")]
        [SerializeField] protected Camera _camera;
        [SerializeField] protected int numInstances = 100;
        [SerializeField] protected bool constructInAwake = false;

        //========== Properties
        public int AvailableInstances { get; private set; } = 0;
        public long CpuTimeMilliseconds { get; private set; } = 0;
        public float AllocatedKB { get; protected set; } = 0;
        public float AllocatedMB { get { return AllocatedKB / 1024; } }
        public bool IsSetup { get; protected set; } = false;

#if UNITY_EDITOR
        /// <summary>
        /// Stopwatch to track how long pre-render takes to compute. Editor only
        /// </summary>
        private Stopwatch _prerenderTimer;
#endif
        private bool _displayPerformance;

        //================ MonoBehaviour

        /// <summary>
        /// Check if we want to construct on awake
        /// </summary>
        protected virtual void Awake()
        {
            _displayPerformance = DEFAULT_PERFORMANCE_ON;
            if (constructInAwake)
                Setup(numInstances);
        }

        /// <summary>
        /// Call pre-render and render
        /// </summary>
        protected virtual void Update()
        {
            if (!IsSetup)
                return;

            if (Input.GetKeyDown(DISPLAY_PERFORMANCE_HOTKEY))
                _displayPerformance = !_displayPerformance;

            //Do pre rendering calculations like culling.
            PreRender();

            //Actually render the meshes.
            Render();
        }

        /// <summary>
        /// On destroy ensure that we deallocate everything
        /// </summary>
        private void OnDestroy()
        {
            //Dispose all of the arrays whenever object is destroyed
            Deallocate();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Displays data about current performance. This is also wrapped in UNITY_EDITOR
        /// </summary>
        private void OnGUI()
        {
            if (_displayPerformance)
            {
                GUI.Box(new Rect(0, 0, 200, 30), "CPU Time: " + CpuTimeMilliseconds + "ms");
                GUI.Box(new Rect(0, 30, 200, 30), "Allocated: " + AllocatedMB.ToString("N1") + "MB (" + AllocatedKB.ToString("N1") + ")");
                GUI.Label(new Rect(0, 60, 500, 30), "Toggle hotkey: " + DISPLAY_PERFORMANCE_HOTKEY + "(InstanceManagerBase.DISPLAY_PERFORMANCE_HOTKEY)");
            }
        }
#endif

        //================ Public / protected
        /// <summary>
        /// Overload to setup that allows you to directly set the camera
        /// </summary>
        /// <param name="instances"></param>
        /// <param name="cam"></param>
        public void Setup(int instances, Camera cam)
        {
            _camera = cam;
            Setup(instances);
        }

        /// <summary>
        /// Handle the pre-render. This can be LOD checking, positional changes, etc.
        /// </summary>
        /// <param name="stopTimer"></param>
        protected virtual void PreRender(bool stopTimer = true)
        {
#if UNITY_EDITOR
            _prerenderTimer.Restart();
#endif
        }

        /// <summary>
        /// Manages the stopwatch for tracking performance with pre-render.
        /// </summary>
        protected virtual void FinishPreRender()
        {
#if UNITY_EDITOR
            _prerenderTimer.Stop();
            CpuTimeMilliseconds = _prerenderTimer.ElapsedMilliseconds;

            if (_prerenderTimer.ElapsedMilliseconds > 1)
                UnityEngine.Debug.Log("Took: " + _prerenderTimer.ElapsedMilliseconds + "ms in prerender.");
#endif
        }

        /// <summary>
        /// Setup the instance manager with X instances available
        /// </summary>
        /// <param name="instances"></param>
        public virtual void Setup(int instances)
        {
            AvailableInstances = instances;
#if UNITY_EDITOR
            _prerenderTimer = new Stopwatch();
#endif
            //If there is no camera, then let us try and find one.
            if (_camera == null)
                TryGetCamera();
        }

        /// <summary>
        /// If there is no camera assigned, then lets just try and get the main camera
        /// </summary>
        private void TryGetCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
                UnityEngine.Debug.LogError("No camera set for InstanceSpawningManager and no camera found in scene to default to. Ensure a camera is setup.");
            else
                UnityEngine.Debug.LogWarning("No camera set for InstancingSpawningManager. Using Camera.main");
        }

        /// <summary>
        /// Allocate the native arrays
        /// </summary>
        protected abstract void Allocate(bool finishAllocation = true);

        /// <summary>
        /// Deallocate all of the native arrays
        /// </summary>
        protected abstract void Deallocate();

        /// <summary>
        /// Render all of the meshes
        /// </summary>
        protected abstract void Render();

        /// <summary>
        /// AllocatedKB is not calculated until you call this. This is because as we make subclasses we may want
        /// to allocate more data.
        /// </summary>
        protected void FinishAllocation()
        {
            AllocatedKB /= 1024;
            IsSetup = true;
            UnityEngine.Debug.Log($"<color=cyan>Setup InstanceSpawningManager with {AvailableInstances} instances available. Allocating {(AllocatedKB).ToString("N0")}KB </color>");
        }

    }

}