using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OcclusionSystem
{
    [DisallowMultipleComponent]
    public sealed class SpriteOcclusionFadeView : MonoBehaviour, IOcclusionFadeView
    {
        [SerializeField] private bool includeInactiveRenderers = true;
        [SerializeField] private List<SpriteRenderer> renderers = new();
        [SerializeField] private Ease ease = Ease.OutQuad;

        private readonly List<float> originalAlphas = new();
        private readonly List<Tween> tweens = new();

        private void Awake()
        {
            RefreshRenderersIfNeeded();
            CacheOriginalAlphas();
        }

        private void OnDisable()
        {
            ResetImmediate();
        }

        [ContextMenu("Refresh Renderers")]
        public void RefreshRenderers()
        {
            renderers.Clear();
            GetComponentsInChildren(includeInactiveRenderers, renderers);
            renderers.RemoveAll(renderer => renderer == null);
            CacheOriginalAlphas();
        }

        public void FadeTo(float alpha, float duration)
        {
            TweenTo(Mathf.Clamp01(alpha), Mathf.Max(0f, duration), useOriginalAlpha: false);
        }

        public void Restore(float duration)
        {
            TweenTo(1f, Mathf.Max(0f, duration), useOriginalAlpha: true);
        }

        public void ResetImmediate()
        {
            KillTweens();

            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                float alpha = i < originalAlphas.Count ? originalAlphas[i] : 1f;
                SetRendererAlpha(renderer, alpha);
            }
        }

        private void TweenTo(float alpha, float duration, bool useOriginalAlpha)
        {
            RefreshRenderersIfNeeded();
            EnsureOriginalAlphaCount();
            KillTweens();

            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                float targetAlpha = useOriginalAlpha ? originalAlphas[i] : alpha;
                if (duration <= 0f)
                {
                    SetRendererAlpha(renderer, targetAlpha);
                    continue;
                }

                Tween tween = DOTween
                    .To(() => renderer.color.a, value => SetRendererAlpha(renderer, value), targetAlpha, duration)
                    .SetEase(ease)
                    .SetTarget(renderer);

                tweens.Add(tween);
            }
        }

        private void RefreshRenderersIfNeeded()
        {
            renderers.RemoveAll(renderer => renderer == null);
            if (renderers.Count == 0)
            {
                RefreshRenderers();
            }
        }

        private void CacheOriginalAlphas()
        {
            originalAlphas.Clear();
            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer renderer = renderers[i];
                originalAlphas.Add(renderer == null ? 1f : renderer.color.a);
            }
        }

        private void EnsureOriginalAlphaCount()
        {
            while (originalAlphas.Count < renderers.Count)
            {
                int index = originalAlphas.Count;
                SpriteRenderer renderer = renderers[index];
                originalAlphas.Add(renderer == null ? 1f : renderer.color.a);
            }
        }

        private void KillTweens()
        {
            for (int i = 0; i < tweens.Count; i++)
            {
                tweens[i]?.Kill();
            }

            tweens.Clear();
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
