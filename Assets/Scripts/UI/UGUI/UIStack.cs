/***************************************************************************
// File       : UIStack.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] UIStack：管理某一 Layer 的实例栈（Push/Pop/Top）
using System.Collections.Generic;

public class UIStack
{
    private readonly List<UIElement> _items = new List<UIElement>();

    public UIElement Top()
    {
        int n = _items.Count;
        if (n <= 0) return null;
        return _items[n - 1];
    }

    public void Push(UIElement e)
    {
        if (e == null) return;
        UIElement current = Top();
        if (current != null) current.OnBlur();
        _items.Add(e);
        e.OnFocus();
    }

    public void Remove(UIElement e)
    {
        if (e == null) return;
        int idx = _items.IndexOf(e);
        if (idx < 0) return;
        bool wasTop = idx == _items.Count - 1;
        _items.RemoveAt(idx);
        if (wasTop)
        {
            UIElement now = Top();
            if (now != null) now.OnFocus();
        }
    }
}
