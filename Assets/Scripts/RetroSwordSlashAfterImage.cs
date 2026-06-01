using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

public class RetroSwordSlashAfterImage : MonoBehaviour
{
    [SerializeField] private bool spawnSlashAfterImages = false;
    [SerializeField] private bool suppressSlashAfterImagesDuringRage = true;
    [SerializeField] private Sprite slashSprite;
    [SerializeField] private SpriteRenderer slashSource;
    [SerializeField] private int slashCount = 4;
    [SerializeField] private float slashSpacing = 0.035f;
    [SerializeField] private float lifetime = 0.14f;
    [SerializeField] private Color startColor = new Color(0.78f, 0.96f, 1f, 0.82f);
    [SerializeField] private Color endColor = new Color(0.08f, 0.02f, 0.16f, 0f);
    [SerializeField] private int sortingOrderOffset = 1;

    private Weapon[] _weapons;
    private Weapon.WeaponStates[] _lastStates;
    private RetroRageModeAnimator _rageModeAnimator;

    private void Awake()
    {
        _weapons = GetComponents<Weapon>();
        _lastStates = new Weapon.WeaponStates[_weapons.Length];

        for (int i = 0; i < _weapons.Length; i++)
        {
            if ((_weapons[i] != null) && (_weapons[i].WeaponState != null))
            {
                _lastStates[i] = _weapons[i].WeaponState.CurrentState;
            }
        }
    }

    private void LateUpdate()
    {
        if ((_weapons == null) || (_weapons.Length == 0))
        {
            return;
        }

        for (int i = 0; i < _weapons.Length; i++)
        {
            Weapon weapon = _weapons[i];
            if ((weapon == null) || (weapon.WeaponState == null))
            {
                continue;
            }

            Weapon.WeaponStates currentState = weapon.WeaponState.CurrentState;
            if ((currentState == Weapon.WeaponStates.WeaponUse) && (_lastStates[i] != currentState))
            {
                StartCoroutine(SpawnSlashBurst(weapon, i));
            }

            _lastStates[i] = currentState;
        }
    }

    private IEnumerator SpawnSlashBurst(Weapon weapon, int comboIndex)
    {
        if (!spawnSlashAfterImages || IsRageModeActive(weapon))
        {
            yield break;
        }

        yield return null;

        if (IsRageModeActive(weapon))
        {
            yield break;
        }

        int count = Mathf.Max(1, slashCount);
        for (int i = 0; i < count; i++)
        {
            SpawnSlash(weapon);

            if (slashSpacing > 0f)
            {
                yield return new WaitForSeconds(slashSpacing);
            }
        }
    }

    private void SpawnSlash(Weapon weapon)
    {
        SpriteRenderer source = GetSlashSource(weapon);
        Sprite sprite = (source != null) ? source.sprite : slashSprite;
        if (sprite == null)
        {
            return;
        }

        GameObject slash = new GameObject("RetroSwordSlashAfterImage");
        slash.transform.position = (source != null) ? source.transform.position : weapon.transform.position;
        slash.transform.rotation = (source != null) ? source.transform.rotation : weapon.transform.rotation;
        slash.transform.localScale = (source != null) ? source.transform.lossyScale : weapon.transform.lossyScale;

        SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = startColor;
        renderer.flipX = (source != null) && source.flipX;
        renderer.flipY = (source != null) && source.flipY;

        SpriteRenderer sortingRenderer = (source != null) ? source : GetSourceRenderer(weapon);
        if (sortingRenderer != null)
        {
            renderer.sortingLayerID = sortingRenderer.sortingLayerID;
            renderer.sortingOrder = sortingRenderer.sortingOrder + sortingOrderOffset;
            renderer.sharedMaterial = sortingRenderer.sharedMaterial;
        }

        StartCoroutine(FadeSlash(slash, renderer));
    }

    private SpriteRenderer GetSlashSource(Weapon weapon)
    {
        if (slashSource != null)
        {
            return slashSource;
        }

        if ((weapon.Owner != null) && (weapon.Owner.CharacterModel != null))
        {
            Transform found = weapon.Owner.CharacterModel.transform.Find("RetroSwordSlash");
            if (found != null)
            {
                SpriteRenderer foundRenderer = found.GetComponent<SpriteRenderer>();
                if (foundRenderer != null)
                {
                    return foundRenderer;
                }
            }
        }

        return null;
    }

    private bool IsRageModeActive(Weapon weapon)
    {
        if (!suppressSlashAfterImagesDuringRage)
        {
            return false;
        }

        if ((_rageModeAnimator == null) && (weapon != null) && (weapon.Owner != null))
        {
            _rageModeAnimator = weapon.Owner.FindAbility<RetroRageModeAnimator>();
        }

        return (_rageModeAnimator != null) && _rageModeAnimator.RageModeActive;
    }

    private SpriteRenderer GetSourceRenderer(Weapon weapon)
    {
        if ((weapon.Owner != null) && (weapon.Owner.CharacterModel != null))
        {
            return weapon.Owner.CharacterModel.GetComponent<SpriteRenderer>();
        }

        return weapon.GetComponentInChildren<SpriteRenderer>();
    }

    private IEnumerator FadeSlash(GameObject slash, SpriteRenderer renderer)
    {
        float duration = Mathf.Max(0.01f, lifetime);
        float elapsed = 0f;
        Vector3 startScale = slash.transform.localScale;

        while ((elapsed < duration) && (renderer != null))
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            renderer.color = Color.Lerp(startColor, endColor, t);
            slash.transform.localScale = startScale;
            yield return null;
        }

        if (slash != null)
        {
            Destroy(slash);
        }
    }
}
