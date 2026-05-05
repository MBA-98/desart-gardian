using UnityEngine;
using UnityEngine.Rendering;

namespace UnityStandardAssets.Water
{
    /// <summary>
    /// Legacy Standard Assets water calls <see cref="Camera.Render"/> from <see cref="MonoBehaviour.OnWillRenderObject"/>.
    /// URP/HDRP do not support nested manual renders here → <c>InvalidOperationException: UniversalCameraData has already been created</c>.
    /// When an SRP is active we skip those renders (reflection may be missing until you use URP water / a Render Feature).
    /// </summary>
    internal static class WaterRenderPipelineCompat
    {
        static bool _loggedSkip;

        /// <summary>True only for the Built-in Render Pipeline (no SRP asset on Graphics or Quality).</summary>
        internal static bool AllowManualCameraRenderInWillRenderObject
        {
            get
            {
                var active = QualitySettings.renderPipeline != null
                    ? QualitySettings.renderPipeline
                    : GraphicsSettings.defaultRenderPipeline;
                return active == null;
            }
        }

        internal static void LogManualRenderSkippedOnce()
        {
            if (_loggedSkip)
                return;
            _loggedSkip = true;
            Debug.LogWarning(
                "[UnityStandardAssets.Water] Skipping reflection/refraction Camera.Render() because a Scriptable Render Pipeline (URP/HDRP) is active. " +
                "Built-in water is not compatible; use URP water or a planar reflection Render Feature. " +
                "This message is shown once.",
                null);
        }
    }
}
