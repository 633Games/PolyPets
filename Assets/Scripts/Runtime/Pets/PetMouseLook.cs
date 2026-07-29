using UnityEngine;

namespace PolyPets.Pets
{
    /// <summary>
    /// Softly turns the pet head toward the mouse, with yaw/pitch clamps so it never snaps its neck.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class PetMouseLook : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private UnityEngine.Camera viewCamera;
        [SerializeField] private bool useMainCamera = true;

        [Header("Limits (degrees from rest pose)")]
        [SerializeField] private float maxYaw = 42f;
        [SerializeField] private float maxPitch = 22f;
        [SerializeField] private float minPitch = -16f;

        [Header("Feel")]
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float mousePlaneBias = 0.15f;
        [SerializeField] private bool lookWhenMouseOffPet = true;
        [SerializeField] private float maxLookDistance = 8f;

        private Quaternion _restLocal;
        private bool _hasRest;
        private float _yaw;
        private float _pitch;

        public Transform Head => head;

        private void Awake()
        {
            ResolveHead();
            CaptureRestPose();
        }

        private void OnEnable()
        {
            ResolveHead();
            CaptureRestPose();
        }

        public void Bind(Transform headBone, UnityEngine.Camera cam = null)
        {
            head = headBone;
            if (cam != null)
                viewCamera = cam;
            CaptureRestPose();
        }

        private void LateUpdate()
        {
            if (head == null)
                ResolveHead();
            if (head == null)
                return;

            if (!_hasRest)
                CaptureRestPose();

            var cam = ResolveCamera();
            if (cam == null)
                return;

            if (!TryGetLookPoint(cam, out Vector3 worldPoint))
            {
                EaseToward(0f, 0f);
                ApplyLocalRotation();
                return;
            }

            Vector3 toTarget = worldPoint - head.position;
            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            // Aim in the pet root's space so 180° spawn yaw is respected.
            Transform basis = transform;
            Vector3 localDir = basis.InverseTransformDirection(toTarget.normalized);

            // Pet meshes face local +Z after the 180° room yaw.
            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float horizontal = new Vector2(localDir.x, localDir.z).magnitude;
            float targetPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.001f, horizontal)) * Mathf.Rad2Deg;

            targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            EaseToward(targetYaw, targetPitch);
            ApplyLocalRotation();
        }

        private void EaseToward(float targetYaw, float targetPitch)
        {
            float t = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            _yaw = Mathf.Lerp(_yaw, targetYaw, t);
            _pitch = Mathf.Lerp(_pitch, targetPitch, t);
        }

        private void ApplyLocalRotation()
        {
            head.localRotation = _restLocal * Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private bool TryGetLookPoint(UnityEngine.Camera cam, out Vector3 worldPoint)
        {
            worldPoint = head.position + transform.forward;

            Vector3 mouse = Input.mousePosition;
            if (float.IsNaN(mouse.x) || float.IsNaN(mouse.y))
                return false;

            // Ignore when cursor is way outside the game view.
            if (!lookWhenMouseOffPet)
            {
                if (mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height)
                    return false;
            }

            Ray ray = cam.ScreenPointToRay(mouse);
            float planeY = head.position.y + mousePlaneBias;
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (!plane.Raycast(ray, out float enter))
            {
                // Fallback: point ahead of camera ray near the pet.
                worldPoint = ray.origin + ray.direction * Vector3.Distance(cam.transform.position, head.position);
                return true;
            }

            worldPoint = ray.GetPoint(enter);
            Vector3 offset = worldPoint - head.position;
            if (offset.sqrMagnitude > maxLookDistance * maxLookDistance)
                worldPoint = head.position + offset.normalized * maxLookDistance;

            return true;
        }

        private void ResolveHead()
        {
            if (head != null)
                return;

            var agent = GetComponent<PetAgent>();
            if (agent != null)
                head = agent.Head;

            if (head == null)
            {
                var found = transform.Find("Head_Box");
                if (found == null)
                {
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Head_Box")
                        {
                            found = t;
                            break;
                        }
                    }
                }

                head = found;
            }
        }

        private UnityEngine.Camera ResolveCamera()
        {
            if (viewCamera != null)
                return viewCamera;
            if (useMainCamera)
                return UnityEngine.Camera.main;
            return null;
        }

        private void CaptureRestPose()
        {
            if (head == null)
                return;
            _restLocal = head.localRotation;
            _yaw = 0f;
            _pitch = 0f;
            _hasRest = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxYaw = Mathf.Clamp(maxYaw, 5f, 80f);
            maxPitch = Mathf.Clamp(maxPitch, 5f, 60f);
            minPitch = Mathf.Clamp(minPitch, -60f, 0f);
            turnSpeed = Mathf.Max(0.1f, turnSpeed);
        }
#endif
    }
}
