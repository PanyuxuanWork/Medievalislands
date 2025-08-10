/***************************************************************************
// File       : PlacementOutline.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// [TODO] 放置预览 - 合并外轮廓：
// 输入：CellRects（每格四角：TL,TR,BR,BL；位于 XZ 平面）
// 算法：把每条边作为“无向边”计数，出现两次的（相邻格共享边）抵消，仅保留出现一次的边；
//      然后把剩余边串联为若干闭合/开口折线，绘制为连贯外轮廓。
// 用法：BuildingPlacer 每帧生成 CellRects → 设置 Placeable → 该组件负责绘制。
[ExecuteAlways]
public class PlacementOutline : MonoBehaviour
{
    [Header("Visual")]
    public bool Placeable = true;
    public float Elevation = 0.03f;        // 抬高，避免与地面 ZFight
    [Range(0.5f, 4f)] public float LineWidth = 2f;
    public Color OkColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color BlockColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Input (from Placer)")]
    // 每个元素为 4 顶点：0=TL, 1=TR, 2=BR, 3=BL
    public List<Vector3[]> CellRects = new List<Vector3[]>();

    // —— 内部缓存：合并后的折线（每条为已按顺序连接的顶点列表）——
    private readonly List<List<Vector3>> _polylines = new List<List<Vector3>>();

    // 为了减少每帧分配，缓存一个字典作为边计数（自定义哈希键）
    private readonly Dictionary<EdgeKey, int> _edgeCount = new Dictionary<EdgeKey, int>(256);

    private void OnValidate()
    {
        if (LineWidth < 0.5f) LineWidth = 0.5f;
    }

    private void LateUpdate()
    {
        // 在运行/编辑时都重建一次轮廓（若有 CellRects）
        RebuildOutline();
    }

    /// <summary>
    /// 根据 CellRects 重建外轮廓折线数据
    /// </summary>
    public void RebuildOutline()
    {
        _polylines.Clear();
        _edgeCount.Clear();

        if (CellRects == null || CellRects.Count == 0) return;

        // 1) 统计无向边：相同边出现两次 → 内部边，剔除；出现一次 → 边界边
        for (int i = 0; i < CellRects.Count; i++)
        {
            Vector3[] r = CellRects[i];
            if (r == null || r.Length != 4) continue;

            Vector3 tl = Lift(r[0]);
            Vector3 tr = Lift(r[1]);
            Vector3 br = Lift(r[2]);
            Vector3 bl = Lift(r[3]);

            AddUndirectedEdge(tl, tr);
            AddUndirectedEdge(tr, br);
            AddUndirectedEdge(br, bl);
            AddUndirectedEdge(bl, tl);
        }

        // 2) 收集边界边，构建顶点邻接（无向图）
        Dictionary<Vector3Key, List<Vector3>> adj = new Dictionary<Vector3Key, List<Vector3>>(256);
        foreach (KeyValuePair<EdgeKey, int> kv in _edgeCount)
        {
            if (kv.Value != 1) continue; // 只保留边界边
            Vector3 a = kv.Key.A.ToVector3();
            Vector3 b = kv.Key.B.ToVector3();
            AddAdj(adj, a, b);
            AddAdj(adj, b, a);
        }

        // 3) 将边界边串联成多条折线（优先形成闭合环）
        HashSet<Vector3Key> visited = new HashSet<Vector3Key>();
        foreach (KeyValuePair<Vector3Key, List<Vector3>> kv in adj)
        {
            if (visited.Contains(kv.Key)) continue;

            // 从该点开始尽力走完一条线（可能闭合）
            List<Vector3> line = BuildPolylineFrom(adj, kv.Key, visited);
            if (line != null && line.Count >= 2)
            {
                _polylines.Add(line);
            }
        }
    }

    private Vector3 Lift(Vector3 v)
    {
        return new Vector3(v.x, v.y + Elevation, v.z);
    }

    private void AddUndirectedEdge(Vector3 a, Vector3 b)
    {
        // 保持方向无关（小→大排序）
        EdgeKey key = EdgeKey.Create(a, b);
        int count;
        if (_edgeCount.TryGetValue(key, out count))
        {
            _edgeCount[key] = count + 1;
        }
        else
        {
            _edgeCount[key] = 1;
        }
    }

    private static void AddAdj(Dictionary<Vector3Key, List<Vector3>> adj, Vector3 a, Vector3 b)
    {
        Vector3Key ka = Vector3Key.From(a);
        List<Vector3> list;
        if (!adj.TryGetValue(ka, out list))
        {
            list = new List<Vector3>(2);
            adj.Add(ka, list);
        }
        list.Add(b);
    }

    private static List<Vector3> BuildPolylineFrom(Dictionary<Vector3Key, List<Vector3>> adj,
                                                   Vector3Key startKey,
                                                   HashSet<Vector3Key> visited)
    {
        // 深/广皆可，这里用“从端点出发尽可能走直”，遇到分叉任选（网格轮廓一般不会复杂到需要更优算法）
        List<Vector3> result = new List<Vector3>(16);

        Vector3Key current = startKey;
        Vector3Key prev = default(Vector3Key);
        bool havePrev = false;

        while (true)
        {
            if (visited.Contains(current)) break;
            visited.Add(current);
            result.Add(current.ToVector3());

            List<Vector3> neighbors;
            if (!adj.TryGetValue(current, out neighbors) || neighbors.Count == 0) break;

            // 选择下一个：优先未访问的；若只有已访问且是起点 → 构成闭合环，收尾
            Vector3Key nextKey = default(Vector3Key);
            bool found = false;

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Key nk = Vector3Key.From(neighbors[i]);
                if (!havePrev || !nk.Equals(prev)) // 不立刻回头
                {
                    nextKey = nk;
                    found = true;
                    // 尽量走未访问的
                    if (!visited.Contains(nk)) break;
                }
            }

            if (!found) break;

            prev = current;
            havePrev = true;
            current = nextKey;

            // 闭合：回到起点
            if (current.Equals(startKey))
            {
                result.Add(current.ToVector3());
                break;
            }
        }

        return result;
    }

    private Color UseColor()
    {
        return Placeable ? OkColor : BlockColor;
    }

    private void OnDrawGizmos()
    {
        if (_polylines == null || _polylines.Count == 0) return;

        Gizmos.color = UseColor();
        for (int i = 0; i < _polylines.Count; i++)
        {
            List<Vector3> line = _polylines[i];
            for (int j = 0; j < line.Count - 1; j++)
            {
                Gizmos.DrawLine(line[j], line[j + 1]);
            }
        }
    }

#if UNITY_EDITOR
    private void OnRenderObject()
    {
        // 在编辑器里用 Handles 画抗锯齿宽线（运行时用 Gizmos 即可）
        if (_polylines == null || _polylines.Count == 0) return;

        Handles.color = UseColor();
        for (int i = 0; i < _polylines.Count; i++)
        {
            List<Vector3> line = _polylines[i];
            if (line.Count < 2) continue;
            Handles.DrawAAPolyLine(LineWidth, line.ToArray());
        }
    }
#endif

    // —— 辅助：带容差的键（只比较 XZ 平面，避免 Y/Elevation 影响；减少浮点误差）——

    private struct Vector3Key
    {
        public int X; public int Z;

        private const float kQuant = 1000f; // 1毫米精度：坐标*1000 后四舍五入为 int

        public static Vector3Key From(Vector3 v)
        {
            return new Vector3Key
            {
                X = Mathf.RoundToInt(v.x * kQuant),
                Z = Mathf.RoundToInt(v.z * kQuant)
            };
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X / kQuant, 0f, Z / kQuant);
        }

        public override int GetHashCode() { return X * 73856093 ^ Z * 19349663; }
        public override bool Equals(object obj)
        {
            if (!(obj is Vector3Key)) return false;
            Vector3Key other = (Vector3Key)obj;
            return X == other.X && Z == other.Z;
        }
        public bool Equals(Vector3Key other) { return X == other.X && Z == other.Z; }
    }

    private struct EdgeKey
    {
        public Vector3Key A; public Vector3Key B;

        public static EdgeKey Create(Vector3 a, Vector3 b)
        {
            Vector3Key ka = Vector3Key.From(a);
            Vector3Key kb = Vector3Key.From(b);
            // 保证 A <= B（字典序），实现“无向边”
            bool swap = (kb.X < ka.X) || (kb.X == ka.X && kb.Z < ka.Z);
            EdgeKey key;
            key.A = swap ? kb : ka;
            key.B = swap ? ka : kb;
            return key;
        }

        public override int GetHashCode() { return A.GetHashCode() * 31 + B.GetHashCode(); }
        public override bool Equals(object obj)
        {
            if (!(obj is EdgeKey)) return false;
            EdgeKey other = (EdgeKey)obj;
            return A.Equals(other.A) && B.Equals(other.B);
        }
    }
}
