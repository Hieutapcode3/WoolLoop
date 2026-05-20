using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityCommunity.UnitySingleton;



#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// DissmissCurrent: show cac popup khac se an di vinh vien
/// PauseCurrent: khi show se an cac popup khac di, khi hide se bat lai cac popup vua an
/// KeepCurrent: khi show se giu nguyen cac popup khac
/// duplicate: sinh ra nhieu popup giong nhau (khong khuyen khich dung cai nay vi chua kiem soat duoc)
/// </summary>
public enum ShowType { DissmissCurrent, PauseCurrent, KeepCurrent, Duplicate, KeepTopPanel, CreateNewFirstIndex }

public abstract class UIPanels<T> : MonoSingleton<UIPanels<T>> where T : Component
{
    public readonly List<Panel> panels = new List<Panel>();

    public RectTransform rootUI;

    [SerializeField]
    private List<Panel> listPanelSource = new List<Panel>(); // prefab duoi asset

    protected List<Panel> _cachedPanels = new List<Panel>();

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                GameObject g = new GameObject(typeof(T).Name);
                instance = g.AddComponent<T>();
                g.AddComponent<GraphicRaycaster>();
                g.AddComponent<Canvas>();
                g.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                g.AddComponent<CanvasScaler>();
                CanvasScaler cs = g.GetComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
#if UNITY_EDITOR
                cs.referenceResolution = Handles.GetMainGameViewSize();
#else
					cs.referenceResolution = new Vector2(Screen.width, Screen.height);
#endif
            }
            return instance;
        }
    }


    public abstract void UpdateActiveScroll();

    public void Hide<T>() where T : Panel
    {
        if (panels.Count == 0)
            return;

        Hide(typeof(T));
    }

    public void HideEmojiBurning(Panel panel)
    {
        if (panels.Count == 0)
            return;

        for (int i = panels.Count - 1; i >= 0; i--)
        {
            if (panels[i] == panel)
            {
                panels[i].Hide();
                // cached panel
            }
        }
    }

    public void HidePanelOnly<T>() where T : Panel
    {
        if (panels.Count == 0)
            return;
        Hide(typeof(T), false);
    }

    public void Hide(Type type, bool checkShowCachePanel = true)
    {
        for (int i = panels.Count - 1; i >= 0; i--)
        {
            if (panels[i].GetType() == type)
            {
                var panel = panels[i];
                panels.RemoveAt(i);

                _cachedPanels.Add(panel);
                panel.Hide();
            }
        }

        // clear cached panel
        ClearPanelCached(type);

        var index = panels.Count - 1;

        //if (index >= 0 && panels[index].isActiveAndEnabled == false && checkShowCachePanel)
        //    panels[index].Show();

        Invoke(nameof(UpdateActiveScroll), 0.1f);
    }

    private void ClearPanelCached(Type type)
    {
        var cachedPanel = _cachedPanels.FindAll(p => p.GetType() == type);
        foreach (var panel in cachedPanel)
        {
            if (panel.isActiveAndEnabled)
                panel.gameObject.SetActive(false);
        }
    }

    public T GetCachedPanel<T>() where T : Panel
    {
        foreach (var pnl in _cachedPanels)
        {
            if (pnl is T t)
                return t;
        }
        return default;
    }

    public void GetCachedPanels<T>(List<T> lst) where T : Panel
    {
        lst.Clear();

        foreach (var pnl in _cachedPanels)
        {
            if (pnl is T t)
                lst.Add(t);
        }
    }

    public T GetActivePanel<T>() where T : Panel
    {
        foreach (var pnl in panels)
        {
            if (pnl is T t)
                return t;
        }
        return default;
    }

    public void GetActivePanels<T>(List<T> lst) where T : Panel
    {
        lst.Clear();

        foreach (var pnl in panels)
        {
            if (pnl is T t)
                lst.Add(t);
        }
    }

    public T Show<T>(ShowType showType = ShowType.KeepCurrent, bool isLastParent = true) where T : Panel
    {
        var p = GetActivePanel<T>();

        if (p && p.gameObject.activeSelf)
        {
            if (showType is ShowType.Duplicate or ShowType.CreateNewFirstIndex)
            {
                p = Instantiate(p, rootUI);
                if (showType == ShowType.CreateNewFirstIndex)
                {
                    panels.Insert(0, p);
                }
                else panels.Add(p);
            }

            // p.Show();
            // p.gameObject.SetActive(true);
            return p;
        }

        p = GetCachedOrCreatePanel<T>(showType);

        if (panels.Count > 0)
        {
            var index = panels.Count - 1;

            // an han popup hien tai
            if (showType == ShowType.DissmissCurrent)
            {
                panels[index].Hide();
                _cachedPanels.Add(panels[index]);

                panels.RemoveAt(index);

            }
            else if (showType == ShowType.PauseCurrent)
            {
                // activing.Peek().Hide(); // tam an popup sau khi hide popup nay se bat lai

                panels[index].Hide();
            }
        }

        if (isLastParent)
            p.transform.SetAsLastSibling();

        p.Show(showType);

        //if (panels.Count != 0)
        //{
        //	if (panels[panels.Count - 1].GetType() == typeof(InternetCheckingPanel))
        //	{
        //		showType = ShowType.KeepTopPanel;

        //	}
        //}

        if (showType == ShowType.KeepTopPanel && panels.Count != 0)
        {
            // var cache = activing.Pop();
            // activing.Push(p);
            // activing.Push(cache);
            panels.Insert(panels.Count - 1, p);
        }
        else if (showType == ShowType.CreateNewFirstIndex)
        {
            panels.Insert(0, p);
        }
        else
            panels.Add(p); //.Push(p);

        UpdateActiveScroll();
        return p;
    }

    public bool CheckScrollCamera()
    {
        foreach (var t in panels)
        {
            if (!t || t.gameObject.activeSelf == false)
                continue;

            // Logs.Info($"Panel {t.gameObject.name} activated. AllowScrollCamera: {t.isAllowScrollCamera}");

            if (t.isAllowScrollCamera == false)
                return false;
        }
        return true;
    }

    // an toan bo popup
    public void HideAll()
    {
        if (panels.Count == 0) return;

        for (int i = panels.Count - 1; i >= 0; i--)
        {
            var p = panels[i];

            if (p)
            {
                p.Hide();
                _cachedPanels.Add(p);
                continue;
            }

            panels.RemoveAt(i);
        }

        // foreach (var p in panels)
        // {
        // 	p.Hide();
        // 	_cachedPanels.Add(p);
        // }

        panels.Clear();
    }

    //public void ShowPopupEmojiAgain(BurningCoinEmoji burningCoinEmoji)
    //{
    //	_cachedPanels.Remove(burningCoinEmoji);
    //	panels.Insert(0, burningCoinEmoji);
    //	burningCoinEmoji.Show();
    //}

    // quay lai popup truoc do
    public void Back<T>() where T : Panel
    {
        if (panels.Count == 0) return;

        if (panels.Count > 0)
        {
            var index = panels.Count - 1;
            var p = panels[index];

            p.Back();
        }

        if (panels.Count > 0)
            panels[panels.Count - 1].Show();
    }

    /// <summary>
    /// Get Panel From Cached Panel or Create new
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private T GetCachedOrCreatePanel<T>(ShowType showType = ShowType.KeepCurrent) where T : Panel
    {
        if (showType != ShowType.CreateNewFirstIndex)
        {
            var index = _cachedPanels.FindIndex(i => i.GetType() == typeof(T));

            if (index > -1)
            {
                var panel = _cachedPanels[index];
                _cachedPanels.RemoveAt(index);

                return (T)panel;
            }
        }


        var findUIInSource = listPanelSource.Find(i => i.GetType() == typeof(T));
        if (findUIInSource == null)
        {
            Debug.LogErrorFormat("Can't find UI Panel with type {0}", typeof(T).Name);
            return null;
        }

        var popup = Instantiate(findUIInSource, rootUI) as T;

        return popup;

    }
}
