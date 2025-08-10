/***************************************************************************
// File       : GridPreviewerEditor.cs
// Author     : Panyuxuan
// Created    : 2025/08/10
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// [TODO] GridPreviewer 的编辑器扩展：
// 在 Scene 视图中高亮鼠标悬停的格子，并可选显示索引标签。
[CustomEditor(typeof(GridPreviewer))]
public class GridPreviewerEditor : Editor
{
    private void OnSceneGUI()
    {
        GridPreviewer previewer = (GridPreviewer)target;
        if (previewer == null || previewer.Asset == null) return;

        Event e = Event.current;
        if (e == null) return;

        // 从 SceneView 鼠标位置发射一条射线
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, previewer.Elevation, 0f));

        float enter;
        if (!plane.Raycast(ray, out enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        Vector2Int cellCoord = previewer.Asset.WorldToCell(hit);
        if (!previewer.Asset.InBounds(cellCoord)) return;

        GridCell cell = previewer.Asset.GetCell(cellCoord);
        if (cell == null) return;

        // 画高亮轮廓
        Handles.color = new Color(1f, 1f, 0f, 0.9f);
        Vector3 tl = cell.GetTopLeft(); tl.y += previewer.Elevation;
        Vector3 tr = cell.GetTopRight(); tr.y += previewer.Elevation;
        Vector3 bl = cell.GetBottomLeft(); bl.y += previewer.Elevation;
        Vector3 br = cell.GetBottomRight(); br.y += previewer.Elevation;

        Handles.DrawAAPolyLine(3f, new Vector3[] { tl, tr, br, bl, tl });

        // 显示索引（可选）
        GridAsset asset = previewer.Asset;
        if (previewer.ShowIndices)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.yellow;
            Handles.Label(cell.WorldCenter + Vector3.up * (0.05f + previewer.Elevation),
                string.Format("({0},{1})", cellCoord.x, cellCoord.y),
                style);
        }

        // 请求重绘（更顺滑）
        SceneView.RepaintAll();
    }
}
#endif
