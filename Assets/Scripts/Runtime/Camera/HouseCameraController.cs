using UnityEngine;
using PolyPets.House;

namespace PolyPets.Camera
{
    /// <summary>
    /// Fixed cozy 3/4 framing for the active room. Designed for a tall small desktop window.
    /// </summary>
    public sealed class HouseCameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Vector3 roomFocusOffset = new(0f, 1.1f, 0f);
        [SerializeField] private Vector3 viewOffset = new(4.2f, 3.4f, -4.2f);
        [SerializeField] private float fieldOfView = 32f;
        [SerializeField] private bool orthographic;
        [SerializeField] private float orthographicSize = 3.2f;

        public UnityEngine.Camera TargetCamera => targetCamera;

        private void Reset()
        {
            targetCamera = GetComponent<UnityEngine.Camera>();
        }

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<UnityEngine.Camera>();

            ApplyLens();
        }

        public void ApplyLens()
        {
            EnsureCamera();
            if (targetCamera == null)
                return;

            targetCamera.orthographic = orthographic;
            if (orthographic)
                targetCamera.orthographicSize = orthographicSize;
            else
                targetCamera.fieldOfView = fieldOfView;

            targetCamera.nearClipPlane = 0.1f;
            targetCamera.farClipPlane = 50f;
        }

        public void FocusRoom(RoomRoot room)
        {
            EnsureCamera();
            if (targetCamera == null || room == null)
                return;

            ApplyLens();

            Vector3 focus = room.FocusPoint + roomFocusOffset;
            Transform cam = targetCamera.transform;
            cam.position = focus + viewOffset;
            cam.rotation = Quaternion.LookRotation(focus - cam.position, Vector3.up);
        }

        private void EnsureCamera()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<UnityEngine.Camera>();
        }
    }
}
