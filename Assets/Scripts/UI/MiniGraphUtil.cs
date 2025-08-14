/***************************************************************************
// File       : MiniGraphUtil.cs
// Author     : Panyuxuan
// Created    : 2025/08/14
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

/***************************************************************************
// File       : MiniGraphUtil.cs
// Description: IMGUI 迷你趋势图（总量趋势）；两面板共用
// ***************************************************************************/

using System.Collections.Generic;
using UnityEngine;

public static class MiniGraphUtil
{
    private static Texture2D _white;
    private static Texture2D White
    {
        get
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }
            return _white;
        }
    }

    /// <summary>在 rect 内绘制一条简单折线（hist 为非负整数序列）</summary>
    public static void DrawLineGraph(Rect rect, IList<int> hist, Color line, Color bg, int maxClamp = -1)
    {
        if (hist == null || hist.Count == 0) return;

        // 背景
        GUI.color = bg;
        GUI.DrawTexture(rect, White);

        // 计算最大值
        int maxVal = 1;
        for (int i = 0; i < hist.Count; i++)
        {
            if (hist[i] > maxVal) maxVal = hist[i];
        }
        if (maxClamp > 0 && maxVal > maxClamp) maxVal = maxClamp;

        // 折线：用若干竖线近似（简单 & 快）
        GUI.color = line;
        float w = rect.width / Mathf.Max(1, hist.Count);
        for (int i = 0; i < hist.Count; i++)
        {
            float h = maxVal > 0 ? (rect.height * (hist[i] / (float)maxVal)) : 0f;
            Rect col = new Rect(rect.x + i * w, rect.y + rect.height - h, Mathf.Max(1f, w - 1f), h);
            GUI.DrawTexture(col, White);
        }
        GUI.color = Color.white;
    }
}
