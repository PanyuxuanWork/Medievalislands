/***************************************************************************
// File       : UIRoot.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] UIRoot：统一创建/持有各 Layer 的 Canvas + GraphicRaycaster + CanvasScaler

using UnityEngine;
using UnityEngine.UI;

namespace UI.UGUI
{
    public class UIRoot : MonoSingleton<UIRoot>
    {
        public Canvas RootCanvas;                 // 挂在场景里或运行时创建
        public CanvasScaler RootScaler;           // 推荐 Scale With Screen Size
        public Camera UICamera;                   // Overlay 模式可为空

        public Transform LayerHUD;
        public Transform LayerPanel;
        public Transform LayerModal;
        public Transform LayerTooltip;
        public Transform LayerSystem;

        protected override void Awake()
        {
            base.Awake();
            if (RootCanvas == null)
            {
                GameObject go = new GameObject("UIRootCanvas");
                RootCanvas = go.AddComponent<Canvas>();
                RootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<GraphicRaycaster>();
                RootScaler = go.AddComponent<CanvasScaler>();
                RootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                RootScaler.referenceResolution = new Vector2(1920, 1080);
            }
            CreateLayer(ref LayerHUD, "HUD", 100);
            CreateLayer(ref LayerPanel, "Panel", 200);
            CreateLayer(ref LayerModal, "Modal", 300);
            CreateLayer(ref LayerTooltip, "Tooltip", 400);
            CreateLayer(ref LayerSystem, "System", 500);
        }

        private void CreateLayer(ref Transform holder, string name, int sorting)
        {
            if (holder != null) return;
            GameObject go = new GameObject("Layer-" + name);
            go.transform.SetParent(RootCanvas.transform, false);
            Canvas c = go.AddComponent<Canvas>();
            c.overrideSorting = true; c.sortingOrder = sorting;
            go.AddComponent<GraphicRaycaster>();
            holder = go.transform;
        }

        public Transform GetLayer(UILayer layer)
        {
            switch (layer)
            {
                case UILayer.HUD: return LayerHUD;
                case UILayer.Panel: return LayerPanel;
                case UILayer.Modal: return LayerModal;
                case UILayer.Tooltip: return LayerTooltip;
                case UILayer.System: return LayerSystem;
            }
            return LayerPanel;
        }
    }
}
