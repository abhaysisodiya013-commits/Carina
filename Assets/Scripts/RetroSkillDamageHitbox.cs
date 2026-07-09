using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class RetroSkillDamageHitbox : MonoBehaviour
    {
        public float DestroyDelay = 0f;

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

                bool isVSpell = GetComponent<RetroSpellCastProjectile>() != null
                                || GetComponentInParent<RetroSpellCastProjectile>() != null
                                || GetComponent<RetroSpellDamageMarker>() != null
                                || GetComponentInParent<RetroSpellDamageMarker>() != null;

                if (isVSpell)
                {
                    ShieldGoatAIController shieldGoat = targetHealth.GetComponent<ShieldGoatAIController>();
                    if (shieldGoat == null)
                    {
                        shieldGoat = targetHealth.GetComponentInParent<ShieldGoatAIController>();
                    }

                    if (shieldGoat != null && shieldGoat.HasShield)
                    {
                        continue;
                    }
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
                    if (DestroyDelay > 0f)
                    {
                        enabled = false; // Stop applying damage!
                        Destroy(gameObject, DestroyDelay);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                    return;
                }
            }
        }

        protected virtual bool IsOwner(Health targetHealth)
        {
            if ((_owner == null) || (targetHealth == null))
            {
                return false;
            }

            if ((targetHealth.gameObject == _owner) || targetHealth.transform.IsChildOf(_owner.transform))
            {
                return true;
            }

            Health ownerHealth = _owner.GetComponentInParent<Health>();
            if ((ownerHealth != null) && (ownerHealth == targetHealth))
            {
                return true;
            }

            Character ownerCharacter = _owner.GetComponentInParent<Character>();
            Character targetCharacter = GetTargetCharacter(targetHealth);
            return (ownerCharacter != null) && (targetCharacter != null) && (ownerCharacter == targetCharacter);
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
            if (!_freezeTargetsOnHit)
            {
                return;
            }

            Character targetCharacter = GetTargetCharacter(targetHealth);
            GameObject targetObj = (targetCharacter != null) ? targetCharacter.gameObject : targetHealth.gameObject;

            RetroTemporaryFreezeEffect freeze = targetObj.GetComponent<RetroTemporaryFreezeEffect>();
            if (freeze == null)
            {
                freeze = targetObj.AddComponent<RetroTemporaryFreezeEffect>();
            }

            freeze.FreezeFor(_freezeDuration, _freezeDelay, _freezeTintColor);
        }

        protected virtual bool CanDamageTarget(Collider2D targetCollider, Health targetHealth)
        {
            Character character = GetTargetCharacter(targetHealth);
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

            return (character != null) && (character.CharacterType == Character.CharacterTypes.AI);
        }

        protected virtual Character GetTargetCharacter(Health targetHealth)
        {
            if (targetHealth == null)
            {
                return null;
            }

            Character character = targetHealth.GetComponent<Character>();
            if (character == null)
            {
                character = targetHealth.GetComponentInParent<Character>();
            }

            return character;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)_areaOffset, _areaSize);
        }
    }

    public class RetroSpellDamageMarker : MonoBehaviour
    {
    }

    public class RetroTemporaryFreezeEffect : MonoBehaviour
    {
        public const float MaxFreezeDuration = 2f;

        protected struct MovementStateSnapshot
        {
            public bool ReadInput;
            public bool AbilityPermitted;
            public bool MovementForbidden;
        }

        protected Character _character;
        protected MoreMountains.Tools.AIBrain _aiBrain;
        protected CharacterHorizontalMovement[] _horizontalMovements;
        protected Animator[] _animators;
        protected SpriteRenderer[] _spriteRenderers;
        protected readonly Dictionary<Animator, float> _storedAnimatorSpeeds = new Dictionary<Animator, float>();
        protected readonly Dictionary<SpriteRenderer, Color> _storedSpriteColors = new Dictionary<SpriteRenderer, Color>();
        protected readonly Dictionary<CharacterHorizontalMovement, MovementStateSnapshot> _storedMovementStates = new Dictionary<CharacterHorizontalMovement, MovementStateSnapshot>();
        protected Coroutine _freezeCoroutine;
        protected bool _isFrozen;
        protected float _restoreAtRealtime = -1f;

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
            duration = Mathf.Min(Mathf.Max(0f, duration), MaxFreezeDuration);
            delay = Mathf.Max(0f, delay);

            if (duration <= 0f)
            {
                return;
            }

            CacheComponents();

            if (_freezeCoroutine != null)
            {
                StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = null;
            }

            if (_isFrozen)
            {
                RestoreSlow();
            }

            _freezeCoroutine = StartCoroutine(FreezeCoroutine(duration, delay, tintColor));
        }

        protected virtual void Update()
        {
            if (_isFrozen && _restoreAtRealtime > 0f && Time.realtimeSinceStartup >= _restoreAtRealtime)
            {
                if (_freezeCoroutine != null)
                {
                    StopCoroutine(_freezeCoroutine);
                    _freezeCoroutine = null;
                }

                RestoreSlow();
            }
        }

        protected virtual IEnumerator FreezeCoroutine(float duration, float delay, Color tintColor)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            ApplySlow(tintColor);
            _restoreAtRealtime = Time.realtimeSinceStartup + duration;
            yield return new WaitForSecondsRealtime(duration);
            RestoreSlow();
            _freezeCoroutine = null;
        }

        protected virtual void ApplySlow(Color tintColor)
        {
            if (_isFrozen)
            {
                ApplyVisualSlow(tintColor);
                return;
            }

            if ((_character != null)
                && (_character.CharacterType == Character.CharacterTypes.Player))
            {
                return;
            }

            CacheMovementState();
            ApplyMovementSlow();

            if (_character != null)
            {
                _character.Freeze();
            }

            if (_aiBrain != null)
            {
                _aiBrain.enabled = false;
            }

            ApplyVisualSlow(tintColor);
            _isFrozen = true;
        }

        protected virtual void CacheMovementState()
        {
            if (_horizontalMovements == null)
            {
                return;
            }

            for (int i = 0; i < _horizontalMovements.Length; i++)
            {
                CharacterHorizontalMovement movement = _horizontalMovements[i];
                if ((movement == null) || _storedMovementStates.ContainsKey(movement))
                {
                    continue;
                }

                _storedMovementStates[movement] = new MovementStateSnapshot
                {
                    ReadInput = movement.ReadInput,
                    AbilityPermitted = movement.AbilityPermitted,
                    MovementForbidden = movement.MovementForbidden
                };
            }
        }

        protected virtual void ApplyMovementSlow()
        {
            if (_horizontalMovements == null)
            {
                return;
            }

            for (int i = 0; i < _horizontalMovements.Length; i++)
            {
                CharacterHorizontalMovement movement = _horizontalMovements[i];
                if (movement == null)
                {
                    continue;
                }

                movement.ReadInput = false;
                movement.AbilityPermitted = false;
                movement.MovementForbidden = true;
                movement.SetHorizontalMove(0f);
            }
        }

        protected virtual void ApplyVisualSlow(Color tintColor)
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

        protected virtual void RestoreSlow()
        {
            RestoreMovementState();

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

            if (_aiBrain != null)
            {
                _aiBrain.enabled = true;
            }

            if (_character != null)
            {
                _character.UnFreeze();
            }

            _restoreAtRealtime = -1f;

            _isFrozen = false;
        }

        protected virtual void RestoreMovementState()
        {
            foreach (KeyValuePair<CharacterHorizontalMovement, MovementStateSnapshot> storedMovement in _storedMovementStates)
            {
                CharacterHorizontalMovement movement = storedMovement.Key;
                if (movement == null)
                {
                    continue;
                }

                MovementStateSnapshot snapshot = storedMovement.Value;
                movement.ReadInput = snapshot.ReadInput;
                movement.AbilityPermitted = snapshot.AbilityPermitted;
                movement.MovementForbidden = snapshot.MovementForbidden;
            }

            _storedMovementStates.Clear();
        }

        protected virtual void CacheComponents()
        {
            _character = GetComponent<Character>();
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
            }

            Transform root = (_character != null) ? _character.transform : transform;
            _aiBrain = root.GetComponent<MoreMountains.Tools.AIBrain>();
            _horizontalMovements = root.GetComponentsInChildren<CharacterHorizontalMovement>();
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
            RestoreSlow();
        }
    }
}
