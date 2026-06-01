using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class RetroSpellCastProjectile : MonoBehaviour
    {
        protected float _direction;
        protected float _speed;
        protected float _distance;
        protected float _travelled;
        protected float _lifetime;
        protected float _minimumVisibleDuration;
        protected float _spawnTime;
        protected bool _distanceReached;

        public virtual void Initialize(float direction, float speed, float distance, float lifetime)
        {
            Initialize(direction, speed, distance, lifetime, 0f);
        }

        public virtual void Initialize(float direction, float speed, float distance, float lifetime, float minimumVisibleDuration)
        {
            _direction = Mathf.Sign(direction);
            _speed = Mathf.Max(0f, speed);
            _distance = Mathf.Max(0f, distance);
            _lifetime = Mathf.Max(0f, lifetime);
            _minimumVisibleDuration = Mathf.Max(0f, minimumVisibleDuration);
            _spawnTime = Time.time;
            float travelDuration = GetTravelDuration();
            float duration = Mathf.Max(_lifetime, travelDuration, _minimumVisibleDuration);
            if (duration > 0f)
            {
                Destroy(gameObject, duration);
            }
        }

        protected virtual void Update()
        {
            if ((_lifetime > 0f) && (Time.time - _spawnTime >= _lifetime) && (Time.time - _spawnTime >= _minimumVisibleDuration))
            {
                Destroy(gameObject);
                return;
            }

            if (_distanceReached)
            {
                return;
            }

            if (_speed <= 0f)
            {
                return;
            }

            float step = _speed * Time.deltaTime;
            if (_distance > 0f)
            {
                step = Mathf.Min(step, Mathf.Max(0f, _distance - _travelled));
            }

            transform.position += Vector3.right * (_direction * step);
            _travelled += step;

            if ((_distance > 0f) && (_travelled >= _distance))
            {
                _distanceReached = true;
            }
        }

        protected virtual float GetTravelDuration()
        {
            if (_speed <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0.01f, _distance / _speed);
        }

    }
}
