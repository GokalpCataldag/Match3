using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    float disappearDuration = 0.25f;

    [SerializeField, Range(0f, 2f)]
    float dropDuration = 0.3f;

    PrefabInstancePool<Tile> pool;
    float disappearProgress;

    bool isDropping = false;
    Vector3 startPosition;
    Vector3 targetPosition;
    float dropProgress;

    public SpriteRenderer spriteRenderer;

    public Sprite defaultIcon;
    public Sprite iconA;
    public Sprite iconB;
    public Sprite iconC;

    public Tile Spawn(Vector3 position)
    {
        Tile instance = pool.GetInstance(this);
        instance.pool = pool;
        instance.transform.localPosition = position;
        instance.transform.localScale = Vector3.one;
        instance.disappearProgress = -1f;
        instance.isDropping = false;
        instance.enabled = false;
        return instance;
    }

    public void Despawn()
    {
        pool.Recycle(this);
    }

    public float Disappear()
    {
        disappearProgress = 0f;
        enabled = true;
        return disappearDuration;
    }

    public float DropTo(Vector3 newPosition)
    {
        startPosition = transform.localPosition;
        targetPosition = newPosition;
        dropProgress = 0f;
        isDropping = true;
        enabled = true;
        return dropDuration;
    }
    public void SetPositionImmediate(Vector3 position)
    {
        transform.localPosition = position;
        isDropping = false;
    }

    void Update()
    {
        if (isDropping)
        {
            dropProgress += Time.deltaTime;
            float t = dropProgress / dropDuration;

            t = 1f - (1f - t) * (1f - t);

            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);

            if (dropProgress >= dropDuration)
            {
                transform.localPosition = targetPosition;
                isDropping = false;
                enabled = false; 
            }
        }

        if (disappearProgress >= 0f)
        {
            disappearProgress += Time.deltaTime;
            if (disappearProgress >= disappearDuration)
            {
                Despawn();
                return;
            }
            transform.localScale = Vector3.one * (1f - disappearProgress / disappearDuration);
        }
    }
    public void SetIconByGroupSize(int groupSize, int a, int b, int c)
    {
        if (groupSize >= c)
            spriteRenderer.sprite = iconC;
        else if (groupSize >= b)
            spriteRenderer.sprite = iconB;
        else if (groupSize >= a)
            spriteRenderer.sprite = iconA;
        else
            spriteRenderer.sprite = defaultIcon;
    }
    public void PlayShuffleEffect()
    {
        StartCoroutine(ShuffleWobble());
    }

    IEnumerator ShuffleWobble()
    {
        Vector3 originalPos = transform.localPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        float strength = 0.1f;

        while (elapsed < duration)
        {
            float offsetX = Mathf.Sin(elapsed * 40f) * strength;
            float offsetY = Mathf.Cos(elapsed * 40f) * strength;
            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}