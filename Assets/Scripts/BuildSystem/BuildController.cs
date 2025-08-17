/***************************************************************************
// File       : BuildController.cs
// Author     : Panyuxuan
// Created    : 2025/08/11
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

using UnityEngine;
using Sirenix.OdinInspector;

// [TODO] 建造控制器：读取 BuildCatalog，监听热键或点击，驱动 BuildingPlacer 放置
public class BuildController : MonoBehaviour
{
    [Header("Refs")]
    public BuildCatalog Catalog;
    public BuildingPlacer Placer;    // 拖你的 BuildingPlacer 上来
    public CityContext City;         // 可选，给 Placer 兜底

    [Header("UI")]
    public bool ShowQuickBar = true;
    public KeyCode ToggleQuickBarKey = KeyCode.Tab;
    public KeyCode PlacerKey = KeyCode.X;

    private void Awake()
    {
        if (Placer == null)
        {
            Placer = GameObject.FindObjectOfType<BuildingPlacer>();
        }
        if (City == null)
        {
            City = GameObject.FindObjectOfType<CityContext>();
        }
    }

    private void Update()
    {
        if (Catalog == null || Catalog.Items == null) return;
        if (Placer == null) return;

        // 快捷键选择
        for (int i = 0; i < Catalog.Items.Length; i++)
        {
            BuildCatalog.Entry e = Catalog.Items[i];
            if (e == null || e.Asset == null) continue;
            if (e.Hotkey != KeyCode.None && Input.GetKeyDown(e.Hotkey))
            {
                StartPlacing(e.Asset);
                break;
            }
        }

        if (Input.GetKeyDown(ToggleQuickBarKey))
        {
            ShowQuickBar = !ShowQuickBar;
        }
        
    }

    [Button("测试：开始放置第一个")]
    public void StartFirst()
    {
        if (Catalog != null && Catalog.Items != null && Catalog.Items.Length > 0 && Catalog.Items[0].Asset != null)
        {
            StartPlacing(Catalog.Items[0].Asset);
        }
    }

    private void OnGUI()
    {
        if (!ShowQuickBar) return;
        if (Catalog == null || Catalog.Items == null) return;

        const float pad = 8f;
        const float btnH = 32f;

        Rect r = new Rect(12, Screen.height - (btnH + pad) - 12, Screen.width - 24, btnH + pad);
        GUILayout.BeginArea(r, GUI.skin.box);
        GUILayout.BeginHorizontal();

        for (int i = 0; i < Catalog.Items.Length; i++)
        {
            BuildCatalog.Entry e = Catalog.Items[i];
            if (e == null || e.Asset == null) continue;

            string keyTip = e.Hotkey != KeyCode.None ? $" [{KeyToLabel(e.Hotkey)}]" : "";
            string costTip = BuildCostTip(e.Asset);
            string label = string.IsNullOrEmpty(e.DisplayName) ? e.Asset.Name : e.DisplayName;
            bool canAfford = CityEconomy.CanAfford(City, e.Asset.BuildCosts);

            GUI.enabled = canAfford;
            if (GUILayout.Button(label + keyTip + costTip, GUILayout.Height(btnH)))
            {
                StartPlacing(e.Asset);
            }
            GUI.enabled = true;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void StartPlacing(BuildingAsset asset)
    {
        if (Placer == null || asset == null) return;
        Placer.SelectedBuilding = asset;
        if (City != null && Placer.City == null) Placer.City = City;
        Placer.StartPlacing();
        TLog.Log(this, "开始放置：" + asset.Name);
    }

    private static string BuildCostTip(BuildingAsset asset)
    {
        if (asset == null || asset.BuildCosts == null || asset.BuildCosts.Count == 0) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder("  (");
        for (int i = 0; i < asset.BuildCosts.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(asset.BuildCosts[i].Type.ToString());
            sb.Append(" x");
            sb.Append(asset.BuildCosts[i].Amount);
        }
        sb.Append(")");
        return sb.ToString();
    }

    private static string KeyToLabel(KeyCode key)
    {
        // 常见数字键美化显示
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
        {
            int n = key - KeyCode.Alpha0;
            return n.ToString();
        }
        return key.ToString();
    }
}
