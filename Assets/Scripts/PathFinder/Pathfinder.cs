/***************************************************************************
// File       : Pathfinder.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 寻路器
// ***************************************************************************/

// [TODO] Pathfinder.cs  (可临时挂在场景一个空物体上)
using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public int PathsPerTick = 8;
    public bool Diagonal = false;

    private readonly Queue<PathRequest> _queue = new Queue<PathRequest>();

    private static readonly Vector2Int[] DIR4 = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
    };
    private static readonly Vector2Int[] DIR8 = {
        new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1),
    };

    void OnEnable()
    {
        TickSystem.OnTick += OnTick; // 复用你现有的固定步长
    }
    void OnDisable()
    {
        TickSystem.OnTick -= OnTick;
    }

    void OnTick()
    {
        int budget = PathsPerTick;
        while (budget-- > 0 && _queue.Count > 0)
        {
            PathRequest req = _queue.Dequeue();
            List<Vector3> path = Solve(req.Start, req.End);
            req.Callback?.Invoke(path);
        }
    }

    public void RequestPath(Vector3 startWorld, Vector3 endWorld, Action<List<Vector3>> cb)
    {
        _queue.Enqueue(new PathRequest { Start = startWorld, End = endWorld, Callback = cb });
    }

    private List<Vector3> Solve(Vector3 startWorld, Vector3 endWorld)
    {
        Vector2Int start, goal;
        if (!GridNavUtil.TryWorldToCell(startWorld, out start)) return null;
        if (!GridNavUtil.TryWorldToCell(endWorld, out goal)) return null;
        if (!GridNavUtil.IsWalkable(start) || !GridNavUtil.IsWalkable(goal)) return null;

        Dictionary<Vector2Int, Vector2Int> came = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();
        List<Vector2Int> open = new List<Vector2Int>();
        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

        gScore[start] = 0;
        open.Add(start);

        Vector2Int[] dirs = Diagonal ? DIR8 : DIR4;

        while (open.Count > 0)
        {
            int bi = 0, bf = int.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                Vector2Int n = open[i];
                int g = gScore.ContainsKey(n) ? gScore[n] : int.MaxValue;
                int h = Mathf.Abs(n.x - goal.x) + Mathf.Abs(n.y - goal.y);
                int f = g + h;
                if (f < bf) { bf = f; bi = i; }
            }
            Vector2Int cur = open[bi];
            open.RemoveAt(bi);

            if (cur == goal) return Reconstruct(cur, came);

            closed.Add(cur);
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2Int nxt = new Vector2Int(cur.x + dirs[i].x, cur.y + dirs[i].y);
                if (!GridNavUtil.IsWalkable(nxt) || closed.Contains(nxt)) continue;

                int candG = (gScore.ContainsKey(cur) ? gScore[cur] : int.MaxValue) + 10;
                if (!gScore.ContainsKey(nxt) || candG < gScore[nxt])
                {
                    gScore[nxt] = candG;
                    came[nxt] = cur;
                    if (!open.Contains(nxt)) open.Add(nxt);
                }
            }
        }
        return null;
    }

    private List<Vector3> Reconstruct(Vector2Int tail, Dictionary<Vector2Int, Vector2Int> came)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        cells.Add(tail);
        while (came.ContainsKey(tail)) { tail = came[tail]; cells.Add(tail); }
        cells.Reverse();

        List<Vector3> world = new List<Vector3>(cells.Count);
        for (int i = 0; i < cells.Count; i++) world.Add(GridNavUtil.CellCenterToWorld(cells[i]));
        return world;
    }

    private struct PathRequest { public Vector3 Start, End; public Action<List<Vector3>> Callback; }
}
