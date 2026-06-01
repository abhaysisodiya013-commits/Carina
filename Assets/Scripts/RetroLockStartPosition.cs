using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Keeps a scene object at the position it had when Play started.
    /// Useful for stationary shooting enemies that should not drift from their placed spot.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Retro Lock Start Position")]
    public class RetroLockStartPosition : MonoBehaviour
    {
        [Tooltip("If true, this object is kept at its Play-start world position.")]
        public bool LockPosition = true;
        [Tooltip("Lock X position.")]
        public bool LockX = true;
        [Tooltip("Lock Y position.")]
        public bool LockY = true;
        [Tooltip("If true, horizontal/vertical velocity is cleared when the position is restored.")]
        public bool ClearVelocity = true;

        protected Vector3 _startPosition;
        protected Rigidbody2D _rigidbody2D;

        protected virtual void Awake()
        {
            _startPosition = transform.position;
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        protected virtual void LateUpdate()
        {
            RestorePosition();
        }

        protected virtual void FixedUpdate()
        {
            RestorePosition();
        }

        protected virtual void RestorePosition()
        {
            if (!LockPosition)
            {
                return;
            }

            Vector3 position = transform.position;
            if (LockX)
            {
                position.x = _startPosition.x;
            }
            if (LockY)
            {
                position.y = _startPosition.y;
            }

            transform.position = position;

            if (ClearVelocity && (_rigidbody2D != null))
            {
                Vector2 velocity = _rigidbody2D.linearVelocity;
                if (LockX)
                {
                    velocity.x = 0f;
                }
                if (LockY)
                {
                    velocity.y = 0f;
                }
                _rigidbody2D.linearVelocity = velocity;
            }
        }
    }
}
