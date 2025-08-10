/***************************************************************************
// File       : MonoSingleton.cs
// Author     : Panyuxuan
// Created    : 2025/08/09
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] 单例基类
// ***************************************************************************/

using UnityEngine;


public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new();
    private static bool _quitting;     // 应用正在退出
    private static bool _loggedAfterQuit;

    /// <summary>
    /// 当前实例是否可用（未退出且实例存在）
    /// </summary>
    public static bool IsAlive => !_quitting && _instance != null;

    /// <summary>
    /// 获取实例；若不存在则会尝试查找或创建。
    /// 在应用退出阶段将返回 null（且不会创建）。
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_quitting)
            {
                // 退出阶段避免误用
                if (!_loggedAfterQuit)
                {
                    _loggedAfterQuit = true;
                    Debug.LogWarning($"[{typeof(T).Name}] Instance requested after quitting. Returning null.");
                }
                return null;
            }

            if (_instance != null) return _instance;

            lock (_lock)
            {
                if (_instance != null) return _instance;

                // 先尝试场景中查找（只找激活对象，够用也更安全）
                var found = FindObjectOfType<T>();
                if (found != null)
                {
                    _instance = found;
                    return _instance;
                }

                // 没找到则自动创建
                var go = new GameObject($"[Singleton] {typeof(T).Name}");
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }
    }

    /// <summary>
    /// 尝试获取实例但不创建；在退出阶段也不会创建。
    /// </summary>
    public static bool TryGet(out T inst)
    {
        inst = (_quitting ? null : _instance ?? FindObjectOfType<T>());
        return inst != null && !_quitting;
    }

    /// <summary>
    /// 是否跨场景保留（默认 true）
    /// </summary>
    protected virtual bool Persistent => true;

    /// <summary>
    /// 初始化回调（子类可选 override）
    /// </summary>
    protected virtual void OnSingletonInit() { }

    protected virtual void Awake()
    {
        // 多例防护
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance found. Destroying this one on {gameObject.name}.");
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;

        if (Persistent)
            DontDestroyOnLoad(gameObject);

        OnSingletonInit();
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // 进入 PlayMode 时确保清理退出标记（处理 Editor 下的域重载/快速进退 Play）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetQuitFlag()
    {
        _quitting = false;
        _loggedAfterQuit = false;
    }
}
