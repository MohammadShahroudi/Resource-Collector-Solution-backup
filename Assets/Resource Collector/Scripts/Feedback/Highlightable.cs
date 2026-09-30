using UnityEngine;

/*
 * Highlightable toggles the authored highlight shader flag on renderers
 * under this object. PlayerController owns when target focus changes; this class
 * only applies the local cosmetic state.
 */

public class Highlightable : MonoBehaviour
{
    static readonly int HighlightEnabledId = Shader.PropertyToID("_Highlight_Enabled");

    Renderer[] _targetRenderers;

    void Awake()
    {
        _targetRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        SetHighlighted(false);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        float highlightEnabled = isHighlighted ? 1f : 0f;

        foreach (Renderer targetRenderer in _targetRenderers)
        {
            Material[] materials = targetRenderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (!material.HasProperty(HighlightEnabledId)) continue;

                material.SetFloat(HighlightEnabledId, highlightEnabled);
            }
        }
    }
}
