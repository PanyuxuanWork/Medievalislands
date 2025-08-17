/***************************************************************************
// File       : IUITransition.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 过渡动画接口（入场/出场）
using System;
public interface IUITransition
{
    void PlayIn(Action onComplete);
    void PlayOut(Action onComplete);
}