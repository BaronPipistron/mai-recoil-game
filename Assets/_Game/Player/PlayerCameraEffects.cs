using UnityEngine;

namespace RecoilArena.Player
{
    public class PlayerCameraEffects : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float baseFov = 80f;
        [SerializeField] private float shotKickFov = 4f;
        [SerializeField] private float recoverSpeed = 10f;
        [SerializeField] private float hitJoltDistance = 0.06f;
        [SerializeField] private float positionRecoverSpeed = 16f;

        private float _currentKick;
        private Vector3 _currentJolt;
        private Vector3 _baseLocalPos;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
            }

            if (targetCamera != null)
            {
                _baseLocalPos = targetCamera.transform.localPosition;
                baseFov = targetCamera.fieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            _currentKick = Mathf.Lerp(_currentKick, 0f, Time.deltaTime * recoverSpeed);
            targetCamera.fieldOfView = baseFov + _currentKick;

            _currentJolt = Vector3.Lerp(_currentJolt, Vector3.zero, Time.deltaTime * positionRecoverSpeed);
            targetCamera.transform.localPosition = _baseLocalPos + _currentJolt;
        }

        public void PlayShotKick(float intensityScale = 1f)
        {
            _currentKick = Mathf.Clamp(_currentKick + shotKickFov * intensityScale, 0f, 12f);
            _currentJolt += new Vector3(0f, -hitJoltDistance * 0.35f, -hitJoltDistance) * intensityScale;
        }

        public void PlayDamageShake(float intensityScale = 1f)
        {
            Vector3 randomJolt = Random.insideUnitSphere * hitJoltDistance * intensityScale;
            _currentJolt += randomJolt;
        }
    }
}
