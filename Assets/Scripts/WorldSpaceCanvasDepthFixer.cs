using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// World Space Canvas의 기본 UI 그래픽이 벽 같은 3D 지오메트리 뒤에서 보이지 않도록 depth test 머티리얼을 적용하는 책임을 가진다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class WorldSpaceCanvasDepthFixer : MonoBehaviour
{
    [SerializeField] private bool includeCustomMaterials = true;

    private readonly Dictionary<Material, Material> depthMaterials = new();
    private readonly List<Graphic> graphics = new();
    private Canvas targetCanvas;

    private void Awake()
    {
        targetCanvas = GetComponent<Canvas>();
        ApplyDepthTestMaterial();
    }

    private void OnEnable()
    {
        ApplyDepthTestMaterial();
    }

    private void OnTransformChildrenChanged()
    {
        if (isActiveAndEnabled)
        {
            ApplyDepthTestMaterial();
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in depthMaterials.Values)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        depthMaterials.Clear();
    }

    private void ApplyDepthTestMaterial()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        if (targetCanvas == null || targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        graphics.Clear();
        GetComponentsInChildren(true, graphics);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }

            if (!includeCustomMaterials && HasCustomMaterial(graphic))
            {
                continue;
            }

            Material material = GetOrCreateDepthTestMaterial(graphic.material);
            if (material == null)
            {
                continue;
            }

            graphic.material = material;
        }
    }

    private Material GetOrCreateDepthTestMaterial(Material source)
    {
        if (source == null)
        {
            source = Graphic.defaultGraphicMaterial;
        }

        if (source == null)
        {
            return null;
        }

        if (depthMaterials.TryGetValue(source, out Material cachedMaterial))
        {
            return cachedMaterial;
        }

        Material material = new(source)
        {
            name = $"{source.name} Depth Tested"
        };

        SetDepthTest(material);
        depthMaterials.Add(source, material);
        return material;
    }

    private static bool HasCustomMaterial(Graphic graphic)
    {
        Material material = graphic.material;
        return material != null && material != Graphic.defaultGraphicMaterial;
    }

    private static void SetDepthTest(Material material)
    {
        int lessEqual = (int)CompareFunction.LessEqual;
        material.SetInt("unity_GUIZTestMode", lessEqual);

        if (material.HasProperty("_ZTest"))
        {
            material.SetInt("_ZTest", lessEqual);
        }

        if (material.HasProperty("_ZTestMode"))
        {
            material.SetInt("_ZTestMode", lessEqual);
        }
    }
}
