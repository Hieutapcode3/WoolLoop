using System;
using System.Collections.Generic;
using UnityEngine;

public class HUDSystem : UIPanels<HUDSystem>
{
	private static bool isLock = false;

	public static bool TapToHideLocked;
	
	private readonly List<Type> _excludeIndexes = new List<Type>();

    protected override void Awake()
    {
		base.Awake();
        if (Instance != this)
        {
            Destroy(this);
            return;
        }
		DontDestroyOnLoad(this);
    }

    private void Start()
	{
		isLock = false;
		TapToHideLocked = false;
		
		//DontDestroyOnLoad(gameObject);
    }


	public static bool IsLock {
		get => isLock;
		set => isLock = value;
	}

	public override void UpdateActiveScroll()
	{
		if (isLock)
			return;

		ScrollingLocked = false == CheckScrollCamera();
		SwipeLocked = ScrollingLocked;
	}

	public void GetActivePanel<T>(List<T> lstPanel) where T : Panel
	{
		lstPanel.Clear();
		foreach (var p in panels)
		{
			if (p is T t)
				lstPanel.Add(t);
		}
	}


	public void HideAllPanelExclude<T>() where T : Panel
	{
		for (int i = 0; i < panels.Count; i++)
		{
			if (panels[i] is T)
				continue;

			Hide(panels[i].GetType());
		}
	}

	public void ShowOrHide<T>(bool showed, out T panel) where T : Panel
	{
		if (showed)
		{
			panel = Show<T>();
			return;
		}

		panel = GetActivePanel<T>();

		Hide<T>();
	}

	/// <summary>
	/// Find Active Panel
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public IEnumerator<T> FindPanel<T>() where T : Panel
	{
		var panel = GetActivePanel<T>();

		while (!panel)
		{
			yield return null;
			panel = GetActivePanel<T>();
		}

		yield return panel;
	}


	public bool ScrollingLocked { get; set; }
	public bool ZoomingLocked { get; set; }
	public bool SwipeLocked { get; set; }
	public bool TouchLocked { get; set; }
	
	/// <summary>
	/// Block By panel
	/// </summary>
	/// <returns></returns>
	public bool BlockByPanel()
	{
		foreach (var panel in panels)
			if (panel.gameObject.activeSelf && panel.blockTouched)
			{
				// Logs.Info($"Block By popup: {panel.name}");
				return true;
			}

		return false;
	}

}