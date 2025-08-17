/***************************************************************************
// File       : UIManager.cs
// Author     : Panyuxuan
// Created    : 2025/08/
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] UIManager：核心路由与动态加载
using System.Collections;
using System.Collections.Generic;
using UI.UGUI;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("Provider 选择：默认 Resources，可切 Addressables")]
    public bool UseAddressables = false;

    private IUIAssetProvider _provider;
    private readonly Dictionary<UILayer, UIStack> _stacks = new Dictionary<UILayer, UIStack>();
    private readonly Dictionary<string, UIElement> _singletons = new Dictionary<string, UIElement>();

    protected override void Awake()
    {
        base.Awake();
        _provider = new UIResourcesProvider();
#if ADDRESSABLES
        if (UseAddressables) _provider = new UIAddressablesProvider();
#endif
        EnsureStacks();
        if (UIRoot.Instance == null) { GameObject go = new GameObject("UIRoot"); go.AddComponent<UIRoot>(); }
    }

    private void EnsureStacks()
    {
        _stacks[UILayer.HUD] = new UIStack();
        _stacks[UILayer.Panel] = new UIStack();
        _stacks[UILayer.Modal] = new UIStack();
        _stacks[UILayer.Tooltip] = new UIStack();
        _stacks[UILayer.System] = new UIStack();
    }

    // 打开：根据 key 动态加载 UI 预制体并实例化到对应层
    public void Open(string key, object args, System.Action<UIElement> onOpened)
    {
        StartCoroutine(CoOpen(key, args, onOpened));
    }

    private IEnumerator CoOpen(string key, object args, System.Action<UIElement> onOpened)
    {
        // Singleton：若存在则直接激活并置顶
        if (_singletons.ContainsKey(key))
        {
            UIElement exists = _singletons[key];
            exists.gameObject.SetActive(true);
            exists.OnOpen(args);
            _stacks[exists.Layer].Push(exists);
            if (onOpened != null) onOpened.Invoke(exists);
            yield break;
        }

        GameObject prefab = null;
        bool loaded = false;
        yield return _provider.LoadAsync(key, p => { loaded = true; prefab = p; });
        if (!loaded || prefab == null)
        {
            Debug.LogError("UIManager.Open 加载失败: " + key);
            yield break;
        }

        UIElement elem = InstantiateOnLayer(prefab);
        elem.OnOpen(args);

        if (elem.IsSingleton) _singletons[key] = elem;
        _stacks[elem.Layer].Push(elem);

        IUITransition trans = elem.GetComponent<IUITransition>();
        if (trans != null) trans.PlayIn(null);

        if (onOpened != null) onOpened.Invoke(elem);
    }

    private UIElement InstantiateOnLayer(GameObject prefab)
    {
        UIElement elem = prefab.GetComponent<UIElement>();
        if (elem == null)
        {
            Debug.LogWarning("预制体缺少 UIElement 组件: " + prefab.name);
        }
        UILayer layer = elem != null ? elem.Layer : UILayer.Panel;
        Transform parent = UIRoot.Instance.GetLayer(layer);
        GameObject go = GameObject.Instantiate(prefab, parent, false);
        UIElement inst = go.GetComponent<UIElement>();
        if (inst == null) inst = go.AddComponent<UIStubElement>();
        return inst;
    }

    public void Close(UIElement elem)
    {
        if (elem == null) return;
        StartCoroutine(CoClose(elem));
    }

    private IEnumerator CoClose(UIElement elem)
    {
        IUITransition trans = elem.GetComponent<IUITransition>();
        bool done = false;
        if (trans != null)
        {
            trans.PlayOut(() => { done = true; });
            while (!done) yield return null;
        }
        UILayer layer = elem.Layer;
        _stacks[layer].Remove(elem);
        elem.OnClose();
        GameObject.Destroy(elem.gameObject);
    }

    // 快捷：打开面板（无回调）
    public void Open(string key, object args)
    {
        Open(key, args, null);
    }

    // 快捷：打开无参面板
    public void Open(string key)
    {
        Open(key, null, null);
    }

    // 快捷：关闭最上层（用于 ESC ）
    public bool CloseTop()
    {
        // Modal > Panel > HUD 顺序尝试
        UIElement top = _stacks[UILayer.Modal].Top();
        if (top == null) top = _stacks[UILayer.Panel].Top();
        if (top == null) return false;
        Close(top);
        return true;
    }

    // 状态快照（仅记录 Key，复杂面板可自定义状态对象）
    public UIStateSnapshot Snapshot()
    {
        UIStateSnapshot s = new UIStateSnapshot();
        // 可扩展：记录每层的所有元素及其自定义状态
        return s;
    }

    public void Restore(UIStateSnapshot snapshot)
    {
        // TODO：按需要恢复 UI
    }

    // 占位：若实例化的预制没有 UIElement 基类，补一个简单实现防止报错
    public class UIStubElement : UIElement { }

    public class UIStateSnapshot { }

}