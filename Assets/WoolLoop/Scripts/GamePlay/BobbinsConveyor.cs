using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class BobbinsBoxSetUp
{
    public WoolColorType boxColorType;
    public BobbinsBoxSize boxSize = BobbinsBoxSize.Size_8;
    public int itemCount;
}

public class BobbinsConveyor : MonoBehaviour
{
    [TitleGroup("Prefab")]
    [SerializeField] private GameObject bobbinsPrefab;
    [TitleGroup("Layout")]
    [SerializeField, Min(0.01f)] private float conveyorScale = 1f;
    // [OnValueChanged(nameof(RefreshLayout))]
    [SerializeField, Min(0.01f)] private float spacingZ = 0.15f;
    [SerializeField, Min(1)] private int visibleCount = 4;
    [SerializeField] private Transform boxSpawnRoot;
    [SerializeField] private Transform bottomLinePoint;
    [SerializeField] private Transform barrierPoint;
    public int VisibleCount => visibleCount;
    [TitleGroup("Setup")]
    [TableList(ShowIndexLabels = true)]
    [SerializeField] private List<BobbinsBoxSetUp> bobbinsConfig = new();
    private readonly List<BobbinsBoxSetUp> _spawnOrder = new();
    private readonly List<GameObject> _allBobbins = new();
    [SerializeField, ReadOnly] private BobbinsBox currentBottomLineBobbinsBox;
    public BobbinsBox CurrentBottomLineBobbinsBox => currentBottomLineBobbinsBox;
    private bool _isRefreshing;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (PrefabUtility.IsPartOfPrefabAsset(this))
            return;
        if (!Application.isPlaying)
        {
            UpdateExistingLayoutInEditMode();
        }
    }
#endif
    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        Rebuild();
    }
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

    private void UpdateExistingLayoutInEditMode()
    {
        if (bobbinsPrefab == null)
            return;

        BuildOrder();

        if (_allBobbins.Count == 0)
            CacheExistingChildren();

        for (int i = 0; i < _allBobbins.Count && i < _spawnOrder.Count; i++)
            UpdateInstance(i);
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
            CleanupMissingInstances();

        while (_allBobbins.Count > targetCount)
            RemoveInstance(_allBobbins.Count - 1);

        while (_allBobbins.Count < targetCount)
            CreateInstance();

        for (int i = 0; i < _allBobbins.Count; i++)
            UpdateInstance(i);

    }

    private void CreateInstance()
    {
        int index = _allBobbins.Count;
        GameObject bobbins = Instantiate(bobbinsPrefab, boxSpawnRoot);
        _allBobbins.Add(bobbins);
    }

    private void UpdateInstance(int index)
    {
        GameObject bobbins = _allBobbins[index];
        if (bobbins == null)
            return;

        Vector3 localPosition = CalculateLocalPosition(index);
        bobbins.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);
        bobbins.transform.localScale = Vector3.one * conveyorScale;
        bobbins.name = $"Bobbin_{index}_Color_{_spawnOrder[index].boxColorType}";

        if (bobbins.TryGetComponent(out BobbinsBox bobbinsBox))
        {
            var boxIndex = _spawnOrder[index];
            bool isBottomLine = index == 0;

            if (Application.isPlaying)
            {
                bobbinsBox.InitBobbinsBox(boxIndex.boxColorType, boxIndex.boxSize, bottomLine: isBottomLine);
                if (isBottomLine)
                    currentBottomLineBobbinsBox = bobbinsBox;
            }
            else
            {
                bobbinsBox.ApplyEditModePreview(boxIndex.boxColorType, boxIndex.boxSize, bottomLine: isBottomLine);
                if (isBottomLine)
                    currentBottomLineBobbinsBox = bobbinsBox;
            }
        }

        bobbins.SetActive(index < visibleCount);
    }

    private Vector3 CalculateLocalPosition(int index)
    {
        float bottomLineZ = bottomLinePoint != null ? bottomLinePoint.localPosition.z : 0f;
        float barrierZ = barrierPoint != null ? barrierPoint.localPosition.z : bottomLineZ;

        if (index <= 0)
            return new Vector3(0f, 0f, bottomLineZ);

        float startZ = barrierZ;
        float z = startZ + (index - 1) * spacingZ;

        if (z < bottomLineZ)
            z = bottomLineZ;

        return new Vector3(0f, 0f, z);
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

        if (_allBobbins.Count == 0 && boxSpawnRoot != null && boxSpawnRoot.childCount > 0)
        {
            for (int i = 0; i < boxSpawnRoot.childCount; i++)
            {
                Transform child = boxSpawnRoot.GetChild(i);
                if (child != null)
                    _allBobbins.Add(child.gameObject);
            }
        }
    }

    private void CacheExistingChildren()
    {
        if (boxSpawnRoot == null)
            return;

        _allBobbins.Clear();
        for (int i = 0; i < boxSpawnRoot.childCount; i++)
        {
            Transform child = boxSpawnRoot.GetChild(i);
            if (child != null)
                _allBobbins.Add(child.gameObject);
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
                    boxSize = setup.boxSize,
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
        currentBottomLineBobbinsBox = null;
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
