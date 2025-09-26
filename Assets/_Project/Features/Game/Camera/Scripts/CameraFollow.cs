using UnityEngine;

namespace TypingSurvivor.Features.Game.Camera
{
    /// <summary>
    /// 指定されたターゲットをスムーズに追従するシンプルなカメラコントローラー。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target { get; set; }

        [SerializeField]
        private float _smoothSpeed = 0.125f;
        [SerializeField]
        private Vector3 _offset = new Vector3(0, 0, -10);

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desiredPosition = Target.position + _offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}
