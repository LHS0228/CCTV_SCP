using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws the original button's clipped-corner outline as UI geometry.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AngledButtonFrame : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = GetPixelAdjustedRect();
        float inset = Mathf.Min(14f, rect.height * 0.2f);
        Vector2[] points =
        {
            new Vector2(rect.xMin + inset, rect.yMin), new Vector2(rect.xMax - inset, rect.yMin),
            new Vector2(rect.xMax, rect.yMin + inset), new Vector2(rect.xMax, rect.yMax - inset),
            new Vector2(rect.xMax - inset, rect.yMax), new Vector2(rect.xMin + inset, rect.yMax),
            new Vector2(rect.xMin, rect.yMax - inset), new Vector2(rect.xMin, rect.yMin + inset)
        };
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[(i + 1) % points.Length];
            Vector2 delta = (end - start).normalized;
            Vector2 normal = new Vector2(-delta.y, delta.x);
            int first = helper.currentVertCount;
            helper.AddVert(start + normal, color, Vector2.zero);
            helper.AddVert(start - normal, color, Vector2.zero);
            helper.AddVert(end - normal, color, Vector2.zero);
            helper.AddVert(end + normal, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
