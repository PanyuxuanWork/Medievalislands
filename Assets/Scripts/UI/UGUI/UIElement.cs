/***************************************************************************
// File       : UIElement.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO]  UIElement：所有 UI 预制体的基类
// ***************************************************************************/

using UI.UGUI;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public UILayer Layer = UILayer.Panel;
    public bool IsSingleton = false;     // 同一 Key 是否仅允许一个实例
    public bool IsModal = false;         // 是否需要遮罩与焦点锁定

    protected object _openArgs;          // 打开时携带的参数
    protected bool _isOpen;

    public virtual void OnOpen(object args)
    {
        _openArgs = args; _isOpen = true; gameObject.SetActive(true);
    }

    public virtual void OnClose()
    {
        _isOpen = false; gameObject.SetActive(false);
    }

    public virtual void OnFocus() { }
    public virtual void OnBlur() { }
}
