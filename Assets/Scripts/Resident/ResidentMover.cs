/***************************************************************************
// File       : ResidentMover.cs
// Author     : Panyuxuan
// Created    : 2025/08/12
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 移动执行器
// ***************************************************************************/

// [TODO] ResidentMover.cs
using System.Collections.Generic;
using UnityEngine;

public class ResidentMover : MonoBehaviour
{
    public float MoveSpeed = 3f;
    public float Reach = 0.05f;

    private List<Vector3> _path;
    private int _idx = -1;
    private bool _moving = false;
    private Pathfinder _pf;

    void Awake()
    {
        _pf = FindObjectOfType<Pathfinder>();
    }

    public void MoveTo(Vector3 target)
    {
        if (_pf == null) { _moving = false; return; }
        _pf.RequestPath(transform.position, target, OnPathReady);
    }

    private void OnPathReady(List<Vector3> path)
    {
        if (path == null || path.Count == 0) { _moving = false; _path = null; _idx = -1; return; }
        _path = path; _idx = 0; _moving = true;
    }

    void Update()
    {
        if (!_moving || _path == null || _idx < 0 || _idx >= _path.Count) return;

        Vector3 pos = transform.position;
        Vector3 tgt = _path[_idx];
        Vector3 next = Vector3.MoveTowards(pos, tgt, MoveSpeed * Time.deltaTime);
        transform.position = next;

        if ((next - tgt).sqrMagnitude <= Reach * Reach)
        {
            _idx++;
            if (_idx >= _path.Count) _moving = false;
        }
    }

    public bool IsMoving() { return _moving; }
}
