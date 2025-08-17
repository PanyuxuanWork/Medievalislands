/***************************************************************************
// File       : UIKey.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] UIKey：用字符串做 Key（也可改成枚举），便于 Addressables/Resources 路径统一
// ***************************************************************************/

using System.Collections;
using UnityEngine;

namespace UI.UGUI
{
    public enum UILayer
    {
        HUD = 0,        // 持久显示，如时间、资源条
        Panel = 1,      // 可同时开多个的普通面板
        Modal = 2,      // 模态对话框（遮罩）
        Tooltip = 3,    // 提示/浮层
        System = 4      // 加载中/黑幕等系统级
    }

    [System.Serializable]
    public class UIKey
    {
        public string Key; // 例如 "ResidentPanel"、"BuildMenu"、"TimeHUD"
    }

// [TODO] 资源提供器接口：可用 Resources 或 Addressables 实现
    public interface IUIAssetProvider
    {
        IEnumerator LoadAsync(string key, System.Action<GameObject> onLoaded);
        void Release(GameObject instance);
    }

// [TODO] Resources 实现（默认方案）：放在 Resources/UI/<Key>.prefab
    public class UIResourcesProvider : IUIAssetProvider
    {
        private const string ROOT = "UI/";

        public IEnumerator LoadAsync(string key, System.Action<GameObject> onLoaded)
        {
            ResourceRequest req = Resources.LoadAsync<GameObject>(ROOT + key);
            yield return req;
            GameObject prefab = req.asset as GameObject;
            if (onLoaded != null) onLoaded.Invoke(prefab);
        }

        public void Release(GameObject instance)
        {
            // Resources 不需要显式释放；若想回收可在此做对象池
        }
    }

// [TODO] Addressables 实现（可选）。未开启 Addressables 可忽略本文件。
// 使用前请安装 Addressables 包，并在 Player Settings 中启用。
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIAddressablesProvider : IUIAssetProvider
{
    public IEnumerator LoadAsync(string key, System.Action<GameObject> onLoaded)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
        yield return handle;
        if (onLoaded != null) onLoaded.Invoke(handle.Result);
    }

    public void Release(GameObject instance)
    {
        Addressables.ReleaseInstance(instance);
    }
}
#endif
}