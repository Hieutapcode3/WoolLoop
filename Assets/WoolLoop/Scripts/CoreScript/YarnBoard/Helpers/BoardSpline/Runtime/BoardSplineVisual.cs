using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

namespace BoardSpline.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineComputer))]
    public sealed class BoardSplineVisual : MonoBehaviour
    {
        [SerializeField] private BoardSplineSettings settings = BoardSplineSettings.Default;
        [SerializeField] private GameObject wallPrefab;

        private readonly List<GameObject> spawnedWalls = new List<GameObject>();

        public void Apply(BoardSplineDataAdapterInfo adapterInfo) => Apply((IBoardSplineDataAdapter)adapterInfo);

        public void Apply(IBoardSplineDataAdapter adapter)
        {
            if (adapter == null) return;
            Apply(BoardSplineAnalyzer.Analyze(adapter));
        }

        public void Apply(BoardSplineBuildData buildData)
        {
            BoardSplineComputerRenderer.Render(gameObject, buildData, settings);

            if (!settings.renderInnerWalls || wallPrefab == null) return;

            if (settings.clearExistingInnerWalls)
            {
                ClearSpawnedWalls();
            }

            SpawnInnerWalls(buildData.InnerEmptyPositions);
        }

        public void SetSettings(BoardSplineSettings value) => settings = value;
        public void SetWallPrefab(GameObject value) => wallPrefab = value;

        private void SpawnInnerWalls(IReadOnlyList<Vector3> positions)
        {
            if (positions == null) return;

            for (var i = 0; i < positions.Count; i++)
            {
                var wall = Instantiate(wallPrefab, positions[i], Quaternion.identity, transform);
                spawnedWalls.Add(wall);
            }
        }

        private void ClearSpawnedWalls()
        {
            for (var i = spawnedWalls.Count - 1; i >= 0; i--)
            {
                if (spawnedWalls[i] == null) continue;

                if (Application.isPlaying) Destroy(spawnedWalls[i]);
                else DestroyImmediate(spawnedWalls[i]);
            }

            spawnedWalls.Clear();
        }
    }
}
