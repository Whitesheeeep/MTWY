using UnityEngine;

namespace OcclusionSystem
{
    public interface IOcclusionFadeView
    {
        void FadeTo(float alpha, float duration);
        void Restore(float duration);
        void ResetImmediate();
    }
}
