using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using Input = UnityEngine.Input;

    /// <summary>
    /// A simple free camera to be added to a Unity game object.
    /// 
    /// Keys:
    ///	wasd / arrows	- movement
    ///	q/e 			- up/down (local space)
    ///	r/f 			- up/down (world space)
    ///	pageup/pagedown	- up/down (world space)
    ///	hold shift		- enable fast movement mode
    ///	right mouse  	- enable free look
    ///	mouse			- free look / rotation
    ///     
    /// </summary>
    public class FreeCam : MonoBehaviour
    {
        /// <summary>
        /// Normal speed of camera movement.
        /// </summary>
        public float movementSpeed = 10f;

        /// <summary>
        /// Speed of camera movement when shift is held down,
        /// </summary>
        public float fastMovementSpeed = 100f;

        /// <summary>
        /// Sensitivity for free look.
        /// </summary>
        public float freeLookSensitivity = 3f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel.
        /// </summary>
        public float zoomSensitivity = 10f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel (fast mode).
        /// </summary>
        public float fastZoomSensitivity = 50f;

        /// <summary>
        /// Set to true when free looking (on right mouse button).
        /// </summary>
        private bool looking = false;

        private void Update()
        {
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? fastMovementSpeed : this.movementSpeed;

            float x = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1;

            float z = 0;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) x = 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) x = -1;

            float y = 0;
            if (Input.GetKey(KeyCode.Q)) y = 1;
            if (Input.GetKey(KeyCode.E)) y = -1;

            Vector3 movementDelta = new Vector3(x, y, z);

            float globalUp = 0;
            if(Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp)) globalUp = 1;
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown)) globalUp = -1;

            //Handle relative movement
            transform.position += transform.right * movementSpeed * Time.deltaTime * movementDelta.x;
            transform.position += transform.up * movementSpeed * Time.deltaTime * movementDelta.y;
            transform.position += transform.forward * movementSpeed * Time.deltaTime * movementDelta.z;

            //Handle global up
            transform.position += Vector3.up * movementSpeed * Time.deltaTime * globalUp;

            float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
            if (scrollAxis != 0 && !Editor.isInDropDown)
            {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + transform.forward * scrollAxis * zoomSensitivity;
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                StartLooking();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                StopLooking();
            }

            if (looking)
            {
                float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity * Time.deltaTime;
                float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity * Time.deltaTime;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }
        }

        void OnDisable()
        {
            StopLooking();
        }

        /// <summary>
        /// Enable free looking.
        /// </summary>
        public void StartLooking()
        {
            looking = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// Disable free looking.
        /// </summary>
        public void StopLooking()
        {
            looking = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
