using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardGameFeel
{
    public const float AbilityAnimationSeconds = 0.46f;

    private const float WindupSeconds = 0.09f;
    private const float LungeSeconds = 0.13f;
    private const float ImpactSeconds = 0.09f;
    private const float ReturnSeconds = 0.15f;
    private const float LungeDistance = 0.3f;

    private readonly struct RectState
    {
        public readonly RectTransform rect;
        public readonly Vector3 position;
        public readonly Vector2 anchoredPosition;
        public readonly Vector3 scale;
        public readonly Quaternion rotation;

        public RectState(RectTransform value)
        {
            rect = value;
            position = value.position;
            anchoredPosition = value.anchoredPosition;
            scale = value.localScale;
            rotation = value.localRotation;
        }

        public void Restore()
        {
            if (!rect)
                return;

            rect.position = position;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = scale;
            rect.localRotation = rotation;
        }
    }

    public static IEnumerator AnimateAbility(RectTransform source, IReadOnlyList<RectTransform> targets)
    {
        if (!source)
            yield break;

        RectState sourceState = new RectState(source);
        List<RectState> targetStates = CaptureTargets(source, targets);

        yield return Tween(WindupSeconds, t =>
        {
            if (!source)
                return;

            float eased = Smooth(t);
            source.localScale = Vector3.LerpUnclamped(sourceState.scale, sourceState.scale * 0.92f, eased);
            source.localRotation = Quaternion.LerpUnclamped(sourceState.rotation,
                sourceState.rotation * Quaternion.Euler(0f, 0f, -4f), eased);
        });

        if (!source)
            yield break;

        Vector3 destination = sourceState.position;
        if (targets != null && targets.Count > 0 && targets[0])
            destination = Vector3.Lerp(sourceState.position, targets[0].position, LungeDistance);

        yield return Tween(LungeSeconds, t =>
        {
            if (!source)
                return;

            float eased = EaseOutCubic(t);
            source.position = Vector3.LerpUnclamped(sourceState.position, destination, eased);
            source.localScale = Vector3.LerpUnclamped(sourceState.scale * 0.92f, sourceState.scale * 1.08f, eased);
            source.localRotation = Quaternion.LerpUnclamped(
                sourceState.rotation * Quaternion.Euler(0f, 0f, -4f),
                sourceState.rotation * Quaternion.Euler(0f, 0f, 3f), eased);
        });

        yield return Tween(ImpactSeconds, t =>
        {
            float punch = Mathf.Sin(t * Mathf.PI);
            for (int i = 0; i < targetStates.Count; i++)
            {
                RectState target = targetStates[i];
                if (!target.rect)
                    continue;

                float direction = i % 2 == 0 ? 1f : -1f;
                target.rect.anchoredPosition = target.anchoredPosition + Vector2.right * (punch * 5f * direction);
                target.rect.localScale = target.scale * (1f + punch * 0.06f);
                target.rect.localRotation = target.rotation * Quaternion.Euler(0f, 0f, punch * 1.5f * direction);
            }
        });

        for (int i = 0; i < targetStates.Count; i++)
            targetStates[i].Restore();

        yield return Tween(ReturnSeconds, t =>
        {
            if (!source)
                return;

            float eased = Smooth(t);
            source.position = Vector3.LerpUnclamped(destination, sourceState.position, eased);
            source.localScale = Vector3.LerpUnclamped(sourceState.scale * 1.08f, sourceState.scale, eased);
            source.localRotation = Quaternion.LerpUnclamped(
                sourceState.rotation * Quaternion.Euler(0f, 0f, 3f), sourceState.rotation, eased);
        });

        sourceState.Restore();
    }

    public static IEnumerator AnimateReveal(RectTransform card)
    {
        if (!card)
            yield break;

        Vector3 finalScale = card.localScale == Vector3.zero ? Vector3.one : card.localScale;
        Quaternion finalRotation = card.localRotation;
        card.localScale = finalScale * 0.72f;
        card.localRotation = finalRotation * Quaternion.Euler(0f, 0f, -6f);

        yield return Tween(0.22f, t =>
        {
            if (!card)
                return;

            float eased = EaseOutBack(t);
            card.localScale = Vector3.LerpUnclamped(finalScale * 0.72f, finalScale, eased);
            card.localRotation = Quaternion.LerpUnclamped(
                finalRotation * Quaternion.Euler(0f, 0f, -6f), finalRotation, Smooth(t));
        });

        if (card)
        {
            card.localScale = finalScale;
            card.localRotation = finalRotation;
        }
    }

    private static List<RectState> CaptureTargets(RectTransform source, IReadOnlyList<RectTransform> targets)
    {
        List<RectState> result = new List<RectState>();
        if (targets == null)
            return result;

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform target = targets[i];
            if (!target || target == source)
                continue;

            bool duplicate = false;
            for (int j = 0; j < result.Count; j++)
            {
                if (result[j].rect == target)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                result.Add(new RectState(target));
        }

        return result;
    }

    private static IEnumerator Tween(float duration, System.Action<float> update)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            update(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        update(1f);
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
    }
}
