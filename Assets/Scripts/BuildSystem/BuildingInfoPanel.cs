/***************************************************************************
// File       : BuildingInfoPanel.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;

// [TODO] 建筑信息面板（运行时 OnGUI）
// 显示：名称、类型、状态、配方、库存；按钮：暂停/恢复生产
public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI")]
    public bool Visible = true;
    public KeyCode ToggleKey = KeyCode.F1;
    public Rect WindowRect = new Rect(12, 12, 360, 320);

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Visible = !Visible;
    }

    private void OnGUI()
    {
        if (!Visible) return;
        BuildingBase target = BuildingSelectionService.Instance != null ? BuildingSelectionService.Instance.Current : null;
        string title = target != null ? ("Building: " + target.name) : "No Building Selected";
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, title);
    }

    private void DrawWindow(int id)
    {
        BuildingBase target = BuildingSelectionService.Instance != null ? BuildingSelectionService.Instance.Current : null;

        if (target == null)
        {
            GUILayout.Label("请在场景中左键点击建筑以选中。");
            GUI.DragWindow();
            return;
        }

        // 基本信息
        GUILayout.Label("状态: " + target.state);
        if (target.Asset != null)
        {
            GUILayout.Label("类型: " + target.Asset.BuildingType + " / " + target.Asset.BuildingSubType);
            GUILayout.Label("占位: " + target.Asset.Footprint.x + " x " + target.Asset.Footprint.y);
            GUILayout.Label("建造耗时(秒): " + target.Asset.BuildTimeSeconds);
        }

        // 生产建筑信息与控制
        ProductionBuilding pb = target.GetComponent<ProductionBuilding>();
        if (pb != null)
        {
            GUILayout.Space(6);
            GUILayout.Label("—— 生产 ——");
            if (pb.recipe != null)
            {
                GUILayout.Label("周期: " + (pb.cycleSecondsOverride > 0f ? pb.cycleSecondsOverride : pb.recipe.cycleSeconds) + " 秒");
                GUILayout.Label("输入：");
                for (int i = 0; i < pb.recipe.inputs.Length; i++)
                {
                    GUILayout.Label("  " + pb.recipe.inputs[i].type + " x" + pb.recipe.inputs[i].amount +
                                    " | 当前：" + pb.inputInv.Get(pb.recipe.inputs[i].type));
                }
                GUILayout.Label("输出：");
                for (int i = 0; i < pb.recipe.outputs.Length; i++)
                {
                    GUILayout.Label("  " + pb.recipe.outputs[i].type + " x" + pb.recipe.outputs[i].amount +
                                    " | 当前：" + pb.outputInv.Get(pb.recipe.outputs[i].type));
                }
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(pb.Paused ? "▶ 恢复" : "⏸ 暂停", GUILayout.Height(28)))
            {
                pb.SetPaused(!pb.Paused);
            }
            if (GUILayout.Button("日志", GUILayout.Height(28)))
            {
                TLog.Log(pb, "状态=" + (pb.Paused ? "Paused" : "Active") + "，输入/输出库存已打印。");
            }
            GUILayout.EndHorizontal();
        }

        // 仓库显示
        WarehouseBuilding wh = target.GetComponent<WarehouseBuilding>();
        if (wh != null)
        {
            GUILayout.Space(6);
            GUILayout.Label("—— 仓库 ——");
            GUILayout.Label("容量: " + wh.Capacity);
            // 如需列出所有资源，可根据你的 ResourceType 列表枚举
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("F1 显示/隐藏  |  右键清除选择");
        GUI.DragWindow();
    }
}
