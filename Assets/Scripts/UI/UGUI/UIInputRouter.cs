/***************************************************************************
// File       : UIInputRouter.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] UIInputRouter：统一处理返回键/Escape
using UnityEngine;

public class UIInputRouter : MonoBehaviour
{
    public bool EnableEscapeToClose = true;

    private void Update()
    {
        if (!EnableEscapeToClose) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool closed = UIManager.Instance.CloseTop();
            if (!closed)
            {
                // TODO：没有可关闭的 UI 时，可触发系统菜单或暂停
            }
        }
    }
}

