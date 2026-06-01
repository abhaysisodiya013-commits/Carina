using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class RetroSkillDamageHitbox : MonoBehaviour
    {
        protected GameObject _owner;
        protected LayerMask _targetLayerMask;
        protected float _damage;
        protected float _invincibilityDuration;
        protected Vector2 _areaSize;
        protected Vector2 _areaOffset;
        protected float _endTime;
        protected bool _useLifetime;
        protected bool _damageAnyAICharacter;
        protected bool _destroyOnHit;
        protected int _maxHitsPerTarget;
        protected float _hitInterval;
        protected bool _freezeTargetsOnHit;
        protected float _freezeDuration;
        protected float _freezeDelay;
        protected Color _freezeTintColor = Color.white;
        protected readonly Dictionary<Health, int> _hitCounts = new Dictionary<Health, int>();
        protected readonly Dictionary<Health, float> _nextHitTimes = new Dictionary<Health, float>();

        public virtual void Initialize(GameObject owner, LayerMask targetLayerMask, float damage, float invincibilityDuration, Vector2 areaSize, Vector2 areaOffset, float lifetime, bool damageAnyAICharacter, bool destroyOnHit, int maxHitsPerTarget, float hitInterval)
        {
            _owner = owner;
            _targetLayerMask = targetLayerMask;
            _damage = damage;
            _invincibilityDuration = invincibilityDuration;
            _areaSize = areaSize;
            _areaOffset = areaOffset;
            _useLifetime = lifetime > 0f;
            _endTime = Time.time + Mathf.Max(0f, lifetime);
            _damageAnyAICharacter = damageAnyAICharacter;
            _destroyOnHit = destroyOnHit;
            _maxHitsPerTarget = Mathf.Max(1, maxHitsPerTarget);
            _hitInterval = Mathf.Max(0f, hitInterval);
            _hitCounts.Clear();
            _nextHitTimes.Clear();
        }

        public virtual void ConfigureFreezeEffect(bool freezeTargetsOnHit, float freezeDuration, Color freezeTintColor)
        {
            ConfigureFreezeEffect(freezeTargetsOnHit, freezeDuration, 0f, freezeTintColor);
        }

        public virtual void ConfigureFreezeEffect(bool freezeTargetsOnHit, float freezeDuration, float freezeDelay, Color freezeTintColor)
        {
            _freezeTargetsOnHit = freezeTargetsOnHit;
            _freezeDuration = Mathf.Max(0f, freezeDuration);
            _freezeDelay = Mathf.Max(0f, freezeDelay);
            _freezeTintColor = freezeTintColor;
        }

        protected virtual void Update()
        {
            ApplyDamage();

            if (_useLifetime && (Time.time >= _endTime))
            {
                enabled = false;
            }
        }

        protected virtual void ApplyDamage()
        {
            int layerMask = _damageAnyAICharacter ? Physics2D.AllLayers : _targetLayerMask.value;
            Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)transform.position + _areaOffset, _areaSize, transform.eulerAngles.z, layerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Health targetHealth = hits[i].GetComponentInParent<Health>();
                if ((targetHealth == null) || IsOwner(targetHealth) || !CanDamageTarget(hits[i], targetHealth) || !CanHitTarget(targetHealth))
                {
                    continue;
                }

                if (_damage > 0f)
                {
                    Vector3 damageDirection = targetHealth.transform.position - transform.position;
                    float previousHealth = targetHealth.CurrentHealth;
                    targetHealth.Damage(_damage, _owner, 0f, _invincibilityDuration, damageDirection);
                    bool damageWasApplied = targetHealth.CurrentHealth < previousHealth;
                    if (!damageWasApplied)
                    {
                        continue;
                    }
                }

                RegisterHit(targetHealth);
                ApplyFreezeEffect(targetHealth);
                if (_destroyOnHit)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        protected virtual bool IsOwner(Health targetHealth)
        {
            return (_owner != null) && ((targetHealth.gameObject == _owner) || targetHealth.transform.IsChildOf(_owner.transform));
        }

        protected virtual bool CanHitTarget(Health targetHealth)
        {
            int hitCount = _hitCounts.ContainsKey(targetHealth) ? _hitCounts[targetHealth] : 0;
            if (hitCount >= _maxHitsPerTarget)
            {
                return false;
            }

            float nextHitTime = _nextHitTimes.ContainsKey(targetHealth) ? _nextHitTimes[targetHealth] : 0f;
            return Time.time >= nextHitTime;
        }

        protected virtual void RegisterHit(Health targetHealth)
        {
            int hitCount = _hitCounts.ContainsKey(targetHealth) ? _hitCounts[targetHealth] : 0;
            _hitCounts[targetHealth] = hitCount + 1;
            _nextHitTimes[targetHealth] = Time.time + _hitInterval;
        }

        protected virtual void ApplyFreezeEffect(Health targetHealth)
        {
            if (!_freezeTargetsOnHit || (_freezeDuration <= 0f) || (targetHealth == null))
            {
                return;
            }

            RetroTemporaryFreezeEffect freezeEffect = targetHealth.GetComponent<RetroTemporaryFreezeEffect>();
            if (freezeEffect == null)
            {
                freezeEffect = targetHealth.gameObject.AddComponent<RetroTemporaryFreezeEffect>();
            }

            freezeEffect.FreezeFor(_freezeDuration, _freezeDelay, _freezeTintColor);
        }

        protected virtual bool CanDamageTarget(Collider2D targetCollider, Health targetHealth)
        {
            bool layerMatches = ((_targetLayerMask.value & (1 << targetCollider.gameObject.layer)) != 0)
                                || ((_targetLayerMask.value & (1 << targetHealth.gameObject.layer)) != 0);
            if (layerMatches)
            {
                return true;
            }

            if (!_damageAnyAICharacter)
            {
                return false;
            }

            Character character = targetHealth.GetComponent<Character>();
            if (character == null)
            {
                character = targetHealth.GetComponentInParent<Character>();
            }

            return (character != null) && (character.CharacterType == Character.CharacterTypes.AI);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)_areaOffset, _areaSize);
        }
    }

    public class RetroTemporaryFreezeEffect : MonoBehaviour
    {
        protected Character _character;
        protected Animator[] _animators;
        protected SpriteRenderer[] _spriteRenderers;
        protected readonly Dictionary<Animator, float> _storedAnimatorSpeeds = new Dictionary<Animator, float>();
        protected readonly Dictionary<SpriteRenderer, Color> _storedSpriteColors = new Dictionary<SpriteRenderer, Color>();
        protected CharacterStates.CharacterConditions _conditionBeforeFreeze;
        protected Coroutine _freezeCoroutine;
        protected bool _isFrozen;

        protected virtual void Awake()
        {
            CacheComponents();
        }

        public virtual void FreezeFor(float duration, Color tintColor)
        {
            FreezeFor(duration, 0f, tintColor);
        }

        public virtual void FreezeFor(float duration, float delay, Color tintColor)
        {
            if (duration <= 0f)
            {
                return;
            }

            CacheComponents();

            if (_freezeCoroutine != null)
            {
                StopCoroutine(_freezeCoroutine);
            }

            _freezeCoroutine = StartCoroutine(FreezeCoroutine(duration, delay, tintColor));
        }

        protected virtual IEnumerator FreezeCoroutine(float duration, float delay, Color tintColor)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            ApplyFreeze(tintColor);
            yield return new WaitForSeconds(duration);
            RestoreFreeze();
            _freezeCoroutine = null;
        }

        protected virtual void ApplyFreeze(Color tintColor)
        {
            if (_isFrozen)
            {
                ApplyVisualFreeze(tintColor);
                return;
            }

            if ((_character != null) && (_character.ConditionState != null))
            {
                _conditionBeforeFreeze = _character.ConditionState.CurrentState;
                if (_conditionBeforeFreeze != CharacterStates.CharacterConditions.Dead)
                {
                    _character.Freeze();
                }
            }

            ApplyVisualFreeze(tintColor);
            _isFrozen = true;
        }

        protected virtual void ApplyVisualFreeze(Color tintColor)
        {
            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (!_storedAnimatorSpeeds.ContainsKey(animator))
                {
                    _storedAnimatorSpeeds[animator] = animator.speed;
                }
                animator.speed = 0f;
            }

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = _spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                if (!_storedSpriteColors.ContainsKey(spriteRenderer))
                {
                    _storedSpriteColors[spriteRenderer] = spriteRenderer.color;
                }
                spriteRenderer.color = tintColor;
            }
        }

        protected virtual void RestoreFreeze()
        {
            foreach (KeyValuePair<Animator, float> storedAnimator in _storedAnimatorSpeeds)
            {
                if (storedAnimator.Key != null)
                {
                    storedAnimator.Key.speed = storedAnimator.Value;
                }
            }
            _storedAnimatorSpeeds.Clear();

            foreach (KeyValuePair<SpriteRenderer, Color> storedColor in _storedSpriteColors)
            {
                if (storedColor.Key != null)
                {
                    storedColor.Key.color = storedColor.Value;
                }
            }
            _storedSpriteColors.Clear();

            if ((_character != null)
                && (_character.ConditionState != null)
                && (_character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Frozen)
                && (_conditionBeforeFreeze != CharacterStates.CharacterConditions.Dead))
            {
                _character.UnFreeze();
            }

            _isFrozen = false;
        }

        protected virtual void CacheComponents()
        {
            _character = GetComponent<Character>();
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
            }

            Transform root = (_character != null) ? _character.transform : transform;
            _animators = root.GetComponentsInChildren<Animator>();
            _spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>();
        }

        protected virtual void OnDisable()
        {
            if (_freezeCoroutine != null)
            {
                StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = null;
            }
            RestoreFreeze();
        }
    }
}
