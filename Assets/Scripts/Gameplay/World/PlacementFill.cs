/***************************************************************************
// File       : PlacementFill.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using System.Collections.Generic;

// [TODO] 放置预览底部填充：把 footprint 的每个格子生成为薄四边形合并网格，绿/红显示可建状态。
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PlacementFill : MonoBehaviour
{
    [Header("Materials")]
    public Material OkMaterial;
    public Material BlockMaterial;

    [Header("Visual")]
    [Range(0.0f, 0.1f)] public float Elevation = 0.02f; // 抬起避免 ZFighting
    [Range(0.0f, 0.1f)] public float Inset = 0.02f;     // 内缩避免与边框重叠

    private MeshFilter _mf;
    private MeshRenderer _mr;
    private Mesh _mesh;

    private void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "PlacementFillMesh";
            _mesh.MarkDynamic();
        }
        _mf.sharedMesh = _mesh;
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows = false;
        _mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    /// <summary> 根据每个格子的四角（tl,tr,br,bl）重建填充网格，并设置可建材质。 </summary>
    public void Rebuild(List<Vector3[]> cellRects, bool placeable)
    {
        if (_mesh == null) return;

        int quadCount = cellRects != null ? cellRects.Count : 0;
        if (quadCount == 0)
        {
            _mesh.Clear();
            return;
        }

        // 预分配
        int vCount = quadCount * 4;
        int iCount = quadCount * 6;
        Vector3[] verts = new Vector3[vCount];
        Vector2[] uvs = new Vector2[vCount];
        int[] tris = new int[iCount];

        float elev = Elevation;
        float inset = Inset;

        for (int q = 0; q < quadCount; q++)
        {
            // 取四角并做轻微内缩与抬高
            Vector3 tl = cellRects[q][0];
            Vector3 tr = cellRects[q][1];
            Vector3 br = cellRects[q][2];
            Vector3 bl = cellRects[q][3];

            tl.y += elev; tr.y += elev; br.y += elev; bl.y += elev;

            // 沿边向内插值，避免拼缝闪烁
            if (inset > 0f)
            {
                Vector3 ctr = (tl + tr + br + bl) * 0.25f;
                tl = Vector3.Lerp(tl, ctr, inset);
                tr = Vector3.Lerp(tr, ctr, inset);
                br = Vector3.Lerp(br, ctr, inset);
                bl = Vector3.Lerp(bl, ctr, inset);
            }

            int vi = q * 4;
            verts[vi + 0] = tl;
            verts[vi + 1] = tr;
            verts[vi + 2] = br;
            verts[vi + 3] = bl;

            // 简单 UV（可用一张小格纹理）
            uvs[vi + 0] = new Vector2(0, 1);
            uvs[vi + 1] = new Vector2(1, 1);
            uvs[vi + 2] = new Vector2(1, 0);
            uvs[vi + 3] = new Vector2(0, 0);

            int ti = q * 6;
            tris[ti + 0] = vi + 0;
            tris[ti + 1] = vi + 1;
            tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 0;
            tris[ti + 4] = vi + 2;
            tris[ti + 5] = vi + 3;
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();

        // 切材质
        if (placeable && OkMaterial != null) _mr.sharedMaterial = OkMaterial;
        else if (!placeable && BlockMaterial != null) _mr.sharedMaterial = BlockMaterial;
    }
}

