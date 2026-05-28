using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class BobbinsBoxSetUp
{
    public WoolColorType boxColorType;
    public int itemCount;
}

public class BobbinsConveyor : MonoBehaviour
{
    [TitleGroup("Prefab")]
    [SerializeField] private GameObject bobbinsPrefab;
    [TitleGroup("Layout")]
    [SerializeField, Min(0.01f)] private float conveyorScale = 1f;
    // [TitleGroup("Layout")]
    [SerializeField, Min(0.01f)] private float spacingZ = 0.15f;
    // [TitleGroup("Layout")]
    [SerializeField, Min(1)] private int visibleCount = 4;
    [SerializeField] private Transform boxSpawnRoot;
    public int VisibleCount => visibleCount;
    [TitleGroup("Setup")]
    [TableList(ShowIndexLabels = true)]
    [SerializeField] private List<BobbinsBoxSetUp> bobbinsConfig = new();
    private readonly List<BobbinsBoxSetUp> _spawnOrder = new();
    private readonly List<GameObject> _allBobbins = new();
    private bool _isRefreshing;

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshLayout();
    }
#endif

    [Button(ButtonSizes.Large)]
    public void Rebuild()
    {
        RefreshLayout(forceRebuild: true);
    }

    private void RefreshLayout(bool forceRebuild = false)
    {
        if (_isRefreshing)
            return;

        if (bobbinsPrefab == null)
            return;

        _isRefreshing = true;
        try
        {
            BuildOrder();
            SyncInstances(forceRebuild);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void SyncInstances(bool forceRebuild)
    {
        int targetCount = _spawnOrder.Count;

        if (forceRebuild)
        {
            RecycleAll();
            _allBobbins.Clear();
        }
        else
        {
            CleanupMissingInstances();
        }

        while (_allBobbins.Count > targetCount)
        {
            RemoveInstance(_allBobbins.Count - 1);
        }

        while (_allBobbins.Count < targetCount)
        {
            CreateInstance();
        }

        for (int i = 0; i < _allBobbins.Count; i++)
        {
            UpdateInstance(i);
        }
    }

    private void CreateInstance()
    {
        GameObject bobbins = Instantiate(bobbinsPrefab, boxSpawnRoot);
        if (bobbins.TryGetComponent(out BobbinsBox bobbinsBox))
        {
            bobbinsBox.SetColorProperties(_spawnOrder[_allBobbins.Count].boxColorType);
        }

        _allBobbins.Add(bobbins);
    }

    private void UpdateInstance(int index)
    {
        GameObject bobbins = _allBobbins[index];
        if (bobbins == null)
            return;

        float localZ = index * spacingZ;
        bobbins.transform.SetLocalPositionAndRotation(new Vector3(0f, 0f, localZ), Quaternion.identity);
        bobbins.transform.localScale = Vector3.one * conveyorScale;
        bobbins.name = $"Bobbin_{index}_Color_{_spawnOrder[index].boxColorType}";

        if (bobbins.TryGetComponent(out BobbinsBox bobbinsBox))
        {
            bobbinsBox.SetColorProperties(_spawnOrder[index].boxColorType);
        }

        bobbins.SetActive(index < visibleCount);
    }

    private void RemoveInstance(int index)
    {
        if (index < 0 || index >= _allBobbins.Count)
            return;

        GameObject bobbins = _allBobbins[index];
        _allBobbins.RemoveAt(index);

        if (bobbins == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(bobbins);
        else
#endif
            Destroy(bobbins);
    }

    private void CleanupMissingInstances()
    {
        for (int i = _allBobbins.Count - 1; i >= 0; i--)
        {
            if (_allBobbins[i] == null)
                _allBobbins.RemoveAt(i);
        }

        if (_allBobbins.Count == 0 && boxSpawnRoot.childCount > 0)
        {
            for (int i = 0; i < boxSpawnRoot.childCount; i++)
            {
                Transform child = boxSpawnRoot.GetChild(i);
                if (child != null)
                    _allBobbins.Add(child.gameObject);
            }
        }
    }
    private void BuildOrder()
    {
        _spawnOrder.Clear();

        for (int i = 0; i < bobbinsConfig.Count; i++)
        {
            BobbinsBoxSetUp setup = bobbinsConfig[i];
            if (setup == null)
                continue;

            int cnt = Mathf.Max(1, setup.itemCount);
            for (int k = 0; k < cnt; k++)
            {
                _spawnOrder.Add(new BobbinsBoxSetUp
                {
                    boxColorType = setup.boxColorType,
                    itemCount = 1,
                });
            }
        }
    }

    [Button(ButtonSizes.Large)]
    public void ClearChild()
    {
        RecycleAll();
        _allBobbins.Clear();
        _spawnOrder.Clear();
        // _bottomIndex = 0;
    }

    private void RecycleAll()
    {
        if (boxSpawnRoot.childCount == 0)
            return;

        for (int i = boxSpawnRoot.childCount - 1; i >= 0; i--)
        {
            var child = boxSpawnRoot.GetChild(i);
            if (child == null) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
#endif
                Destroy(child.gameObject);
        }
    }
}
