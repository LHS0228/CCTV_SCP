using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 투명 머티리얼을 쓰는 MeshRenderer가 화면에는 보이지만 depth buffer에는 기록되지 않아 UI를 가리지 못하는 문제를 보이지 않는 depth-only 복제 렌더러로 보완하는 책임을 가진다.
/// </summary>
public sealed class TransparentMeshDepthOccluder : MonoBehaviour
{
    private const string OccluderSuffix = "__DepthOccluder";

    [SerializeField] private Material depthOnlyMaterial;
    [SerializeField] private bool includeInactiveRenderers;

    private readonly List<MeshRenderer> sourceRenderers = new();

    private void Awake()
    {
        EnsureOccluders();
    }

    private void OnEnable()
    {
        EnsureOccluders();
    }

    private void EnsureOccluders()
    {
        Material material = GetDepthOnlyMaterial();
        if (material == null)
        {
            return;
        }

        sourceRenderers.Clear();
        GetComponentsInChildren(includeInactiveRenderers, sourceRenderers);

        foreach (MeshRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer == null || sourceRenderer.name.EndsWith(OccluderSuffix))
            {
                continue;
            }

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            MeshRenderer occluderRenderer = GetOrCreateOccluder(sourceRenderer, sourceFilter);
            occluderRenderer.enabled = sourceRenderer.enabled;
            occluderRenderer.sharedMaterial = material;
            occluderRenderer.shadowCastingMode = ShadowCastingMode.Off;
            occluderRenderer.receiveShadows = false;
            occluderRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            occluderRenderer.lightProbeUsage = LightProbeUsage.Off;
            occluderRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            occluderRenderer.rendererPriority = sourceRenderer.rendererPriority - 1;
        }
    }

    private MeshRenderer GetOrCreateOccluder(MeshRenderer sourceRenderer, MeshFilter sourceFilter)
    {
        string occluderName = sourceRenderer.name + OccluderSuffix;
        Transform existingTransform = sourceRenderer.transform.Find(occluderName);
        if (existingTransform != null &&
            existingTransform.TryGetComponent(out MeshRenderer existingRenderer) &&
            existingTransform.TryGetComponent(out MeshFilter existingFilter))
        {
            existingFilter.sharedMesh = sourceFilter.sharedMesh;
            return existingRenderer;
        }

        GameObject occluder = new(occluderName);
        occluder.hideFlags = HideFlags.DontSave;
        occluder.layer = sourceRenderer.gameObject.layer;
        occluder.transform.SetParent(sourceRenderer.transform, false);
        occluder.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        occluder.transform.localScale = Vector3.one;

        MeshFilter meshFilter = occluder.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceFilter.sharedMesh;

        return occluder.AddComponent<MeshRenderer>();
    }

    private Material GetDepthOnlyMaterial()
    {
        if (depthOnlyMaterial != null)
        {
            return depthOnlyMaterial;
        }

        Shader shader = Shader.Find("Hidden/CCTV/DepthOnlyOccluder");
        if (shader == null)
        {
            return null;
        }

        depthOnlyMaterial = new Material(shader)
        {
            name = "Runtime Depth Only Occluder"
        };
        return depthOnlyMaterial;
    }
}
