/***************************************************************************
// File       : UIFadeTransition.cs
// Author     : Panyuxuan
// Created    : 2025/08/17
// Copyright  : © 2025 SkyWander Games. All rights reserved.
// Description: [TODO] Add script summary here
// ***************************************************************************/

// [TODO] 简易淡入淡出
using System;
using UnityEngine;

public class UIFadeTransition : MonoBehaviour, IUITransition
{
    public CanvasGroup Group;
    public float Duration = 0.15f;

    private void Awake()
    {
        if (Group == null) Group = GetComponent<CanvasGroup>();
        if (Group == null) Group = gameObject.AddComponent<CanvasGroup>();
    }

    public void PlayIn(Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(CoFade(0f, 1f, onComplete));
    }

    public void PlayOut(Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(CoFade(1f, 0f, onComplete));
    }

    private System.Collections.IEnumerator CoFade(float a, float b, Action onComplete)
    {
        float t = 0f;
        while (t < Duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Duration);
            float v = Mathf.Lerp(a, b, k);
            Group.alpha = v;
            yield return null;
        }
        Group.alpha = b;
        if (onComplete != null) onComplete.Invoke();
    }
}
