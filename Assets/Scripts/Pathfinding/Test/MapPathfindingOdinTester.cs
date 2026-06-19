#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using GameData;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using WS_Modules;

namespace Pathfinding
{
    /// <summary>
    /// 基于 Odin Inspector 的地图寻路手动测试组件，用于加载测试地图、触发 A* 寻路，并在 SceneView 中可视化路径和计算过程。
    /// </summary>
    [ExecuteAlways]
    public sealed class MapPathfindingOdinTester : MonoBehaviour
    {
        [Title("地图数据")]
        [InfoBox("Edit Mode 下可手动填写 mapId 和场景 Grid 后点击加载测试地图数据；Play Mode 下也可以直接使用运行时已加载的地图。")]
        [SerializeField] private MapGridCatalog_SO catalog = default;
        [SerializeField, WSScene] private string mapId = "01_MainScene";
        [SerializeField] private Grid grid = default;

        [Title("Cell 路径参数")]
        [SerializeField] private Vector3Int startCell;
        [SerializeField] private Vector3Int targetCell;

        [Title("World 路径参数")]
        [SerializeField] private Transform startTransform;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 startWorld;
        [SerializeField] private Vector3 targetWorld;

        [Title("可视化")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private bool showNodeLabels = true;
        [SerializeField] private Color pathColor = new Color(0.16f, 0.64f, 1f, 0.42f);

        [Title("A* 计算过程")]
        [SerializeField] private bool showSearchProcess;
        [SerializeField] private bool showSearchCosts = true;
        [SerializeField] private int processStepIndex;

        [Title("最近结果")]
        [ShowInInspector, ReadOnly] private bool lastSearchSucceeded;
        [ShowInInspector, ReadOnly] private string statusText = "Ready.";
        [ShowInInspector, ReadOnly] private int PathNodeCount => pathCells.Count;
        [ShowInInspector, ReadOnly] private int ProcessStepCount => debugResult.Steps.Count;
        [ShowInInspector, ReadOnly] private string CurrentProcessStep =>
            TryGetCurrentDebugStep(out MapPathfindingDebugStep step)
                ? $"{step.Index}/{ProcessStepCount - 1}: {step.Description}"
                : "No process data.";

        private readonly List<Vector3Int> pathCells = new List<Vector3Int>();
        private readonly List<Vector3> worldPath = new List<Vector3>();
        private readonly MapPathfindingDebugResult debugResult = new MapPathfindingDebugResult();

        /// <summary>
        /// 确保地图 Grid 数据库已注册，并将 Inspector 中的地图数据和 Grid 临时加载到数据库。
        /// </summary>
        [Button("加载测试地图数据", ButtonSizes.Large)]
        public void LoadTestMapData()
        {
            LoadTestMapDataAsync().Forget();
        }

        private async UniTaskVoid LoadTestMapDataAsync()
        {
            if (!TryLoadTestMapData(out string failureReason))
            {
                SetFailure($"加载测试地图数据失败: {failureReason}");
                return;
            }

            bool loaded = await MapGridManager.Instance.LoadCurrentMapAsync(mapId, grid);
            if (!loaded)
            {
                SetFailure($"加载测试地图数据失败: Catalog/Addressables 未能加载 mapId={mapId}.");
                return;
            }

            SetStatus($"加载测试地图数据成功. MapId:{mapId}.");
            Debug.Log($"[MapPathfindingOdinTester] 加载测试地图数据成功 mapId={mapId}, grid={grid.name}");
        }

        /// <summary>
        /// 使用 Inspector 中的起点和终点 cell 执行一次 A* 寻路测试，并记录计算过程。
        /// </summary>
        [Button("查找 Cell 路径", ButtonSizes.Large)]
        public void FindCellPath()
        {
            pathCells.Clear();
            worldPath.Clear();
            processStepIndex = 0;
            debugResult.Clear(startCell, targetCell);

            if (!EnsureMapReady(out MapGridManager mapGrid, out string failureReason))
            {
                SetFailure($"查找 Cell 路径失败: {failureReason}");
                return;
            }

            if (!ValidateCellPathRequest(mapGrid, startCell, targetCell, out failureReason))
            {
                SetFailure($"查找 Cell 路径失败: {failureReason}");
                return;
            }

            bool success = MapPathfindingDebugService.TryFindPathWithDebug(startCell, targetCell, pathCells, debugResult);
            processStepIndex = Mathf.Max(0, debugResult.Steps.Count - 1);
            if (!success)
            {
                SetFailure($"查找 Cell 路径失败: target unreachable. start={startCell}, target={targetCell}");
                return;
            }

            BuildWorldPathFromCells(mapGrid);
            SetSuccess($"查找 Cell 路径成功. start={startCell}, target={targetCell}, nodes={pathCells.Count}, steps={debugResult.Steps.Count}");
        }

        /// <summary>
        /// 使用 Transform 或手填 world 坐标执行一次 A* 寻路测试，并记录计算过程。
        /// </summary>
        [Button("查找 World 路径", ButtonSizes.Large)]
        public void FindWorldPath()
        {
            pathCells.Clear();
            worldPath.Clear();
            processStepIndex = 0;

            if (!EnsureMapReady(out MapGridManager mapGrid, out string failureReason))
            {
                SetFailure($"查找 World 路径失败: {failureReason}");
                return;
            }

            Vector3 resolvedStartWorld = GetWorldInput(startTransform, startWorld);
            Vector3 resolvedTargetWorld = GetWorldInput(targetTransform, targetWorld);
            Vector3Int resolvedStartCell = mapGrid.WorldToCell(resolvedStartWorld);
            Vector3Int resolvedTargetCell = mapGrid.WorldToCell(resolvedTargetWorld);
            debugResult.Clear(resolvedStartCell, resolvedTargetCell);

            if (!ValidateCellPathRequest(mapGrid, resolvedStartCell, resolvedTargetCell, out failureReason))
            {
                SetFailure($"查找 World 路径失败: {failureReason}");
                return;
            }

            bool success = MapPathfindingDebugService.TryFindWorldPathWithDebug(
                resolvedStartWorld,
                resolvedTargetWorld,
                worldPath,
                debugResult);
            processStepIndex = Mathf.Max(0, debugResult.Steps.Count - 1);
            if (!success)
            {
                SetFailure($"查找 World 路径失败: target unreachable. startWorld={resolvedStartWorld}, targetWorld={resolvedTargetWorld}");
                return;
            }

            BuildCellsFromWorldPath(mapGrid);
            startCell = resolvedStartCell;
            targetCell = resolvedTargetCell;
            SetSuccess($"查找 World 路径成功. startCell={resolvedStartCell}, targetCell={resolvedTargetCell}, nodes={pathCells.Count}, steps={debugResult.Steps.Count}");
        }

        /// <summary>
        /// 将当前选择的一个或两个 Transform 写入起点和终点字段。
        /// </summary>
        [Button("使用 Selection 作为起终点")]
        public void UseSelectionAsStartAndTarget()
        {
            Transform[] transforms = Selection.transforms;
            if (transforms == null || transforms.Length == 0)
            {
                SetFailure("使用 Selection 作为起终点失败: 当前没有选中 Transform.");
                return;
            }

            startTransform = transforms[0];
            startWorld = startTransform.position;

            targetTransform = transforms.Length > 1 ? transforms[1] : transforms[0];
            targetWorld = targetTransform.position;

            SetStatus($"Selection 已写入. start={startTransform.name}, target={targetTransform.name}");
        }

        /// <summary>
        /// 清除最近一次路径结果和 SceneView 可视化。
        /// </summary>
        [Button("清除路径")]
        public void ClearPath()
        {
            pathCells.Clear();
            worldPath.Clear();
            processStepIndex = 0;
            debugResult.Clear(startCell, targetCell);
            lastSearchSucceeded = false;
            SetStatus("路径已清除.");
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 将 A* 过程可视化切到第一步。
        /// </summary>
        [Button("过程 第一步")]
        public void ShowFirstProcessStep()
        {
            processStepIndex = 0;
            SetStatus(CurrentProcessStep);
        }

        /// <summary>
        /// 将 A* 过程可视化切到上一步。
        /// </summary>
        [Button("过程 上一步")]
        public void ShowPreviousProcessStep()
        {
            processStepIndex = Mathf.Max(0, processStepIndex - 1);
            SetStatus(CurrentProcessStep);
        }

        /// <summary>
        /// 将 A* 过程可视化切到下一步。
        /// </summary>
        [Button("过程 下一步")]
        public void ShowNextProcessStep()
        {
            processStepIndex = Mathf.Min(Mathf.Max(0, debugResult.Steps.Count - 1), processStepIndex + 1);
            SetStatus(CurrentProcessStep);
        }

        /// <summary>
        /// 将 A* 过程可视化切到最后一步。
        /// </summary>
        [Button("过程 最后一步")]
        public void ShowLastProcessStep()
        {
            processStepIndex = Mathf.Max(0, debugResult.Steps.Count - 1);
            SetStatus(CurrentProcessStep);
        }

        /// <summary>
        /// 打印当前 A* 过程步骤中的 open、closed 和 current 状态。
        /// </summary>
        [Button("打印当前过程步骤")]
        public void PrintCurrentProcessStep()
        {
            if (!TryGetCurrentDebugStep(out MapPathfindingDebugStep step))
            {
                Debug.Log("[MapPathfindingOdinTester] 当前没有 A* 过程数据.");
                return;
            }

            Debug.Log(
                $"[MapPathfindingOdinTester] Step {step.Index}/{ProcessStepCount - 1}: {step.Description}, " +
                $"current={step.CurrentCell}, open={step.OpenCells.Count}, closed={step.ClosedCells.Count}, cameFrom={step.CameFrom.Count}");
        }

        /// <summary>
        /// 打印最近一次路径的 cell 和 world 坐标。
        /// </summary>
        [Button("打印当前路径")]
        public void PrintCurrentPath()
        {
            if (!lastSearchSucceeded || pathCells.Count == 0)
            {
                Debug.Log("[MapPathfindingOdinTester] 当前没有路径.");
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[MapPathfindingOdinTester] 当前路径 nodes={pathCells.Count}");
            for (int i = 0; i < pathCells.Count; i++)
            {
                Vector3 point = i < worldPath.Count ? worldPath[i] : Vector3.zero;
                builder.AppendLine($"{i}: cell={pathCells[i]}, world={point}");
            }

            Debug.Log(builder.ToString());
        }

        /// <summary>
        /// 打印当前 IMapGridDatabase 的注册和加载状态。
        /// </summary>
        [Button("打印地图状态")]
        public void PrintMapStatus()
        {
            if (!GameDatabase.TryGet(out IMapGridDatabase _))
            {
                Debug.LogError("[MapPathfindingOdinTester] IMapGridDatabase 未注册.");
                return;
            }

            MapGridManager mapGrid = MapGridManager.Instance;
            Debug.Log(
                $"[MapPathfindingOdinTester] MapId={mapGrid.CurrentMapId}, HasMapData={mapGrid.CurrentMapData != null}, HasGrid={mapGrid.HasCurrentGrid}");
        }

        private void OnValidate()
        {
            pathColor.a = Mathf.Clamp01(pathColor.a);
            processStepIndex = Mathf.Max(0, processStepIndex);
        }

        private void OnDrawGizmos()
        {
            bool canDrawPath = showPath && lastSearchSucceeded && pathCells.Count > 0;
            bool canDrawProcess = showSearchProcess && debugResult.Steps.Count > 0;
            if (!canDrawPath && !canDrawProcess)
            {
                return;
            }

            if (!TryGetReadyMapGrid(out MapGridManager mapGrid, out _))
            {
                return;
            }

            if (canDrawProcess)
            {
                DrawSearchProcess(mapGrid);
            }

            if (canDrawPath)
            {
                DrawPath(mapGrid);
            }
        }

        private bool EnsureMapReady(out MapGridManager mapGrid, out string failureReason)
        {
            if (TryGetReadyMapGrid(out mapGrid, out _))
            {
                failureReason = string.Empty;
                return true;
            }

            if (!TryLoadTestMapData(out failureReason))
            {
                mapGrid = null;
                return false;
            }

            return TryGetReadyMapGrid(out mapGrid, out failureReason);
        }

        private bool TryLoadTestMapData(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                failureReason = "mapId 未赋值.";
                return false;
            }

            if (grid == null)
            {
                failureReason = "grid 未赋值.";
                return false;
            }

            if (!GameDatabase.TryGet(out IMapGridDatabase mapGrid))
            {
                mapGrid = new MapGridDatabase(catalog);
                GameDatabase.Register<IMapGridDatabase>(mapGrid);
            }

            failureReason = string.Empty;
            return true;
        }

        private static bool TryGetReadyMapGrid(out MapGridManager mapGrid, out string failureReason)
        {
            mapGrid = MapGridManager.Instance;
            if (!GameDatabase.TryGet(out IMapGridDatabase _))
            {
                failureReason = "IMapGridDatabase 未注册.";
                return false;
            }

            if (mapGrid.CurrentMapData == null)
            {
                failureReason = "CurrentMapData 未加载.";
                return false;
            }

            if (!mapGrid.HasCurrentGrid)
            {
                failureReason = "CurrentGrid 未加载.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static bool ValidateCellPathRequest(
            MapGridManager mapGrid,
            Vector3Int start,
            Vector3Int target,
            out string failureReason)
        {
            if (!mapGrid.TryGetCell(start, out _))
            {
                failureReason = $"起点超出当前地图范围: {start}.";
                return false;
            }

            if (!mapGrid.TryGetCell(target, out _))
            {
                failureReason = $"终点超出当前地图范围: {target}.";
                return false;
            }

            if (!mapGrid.IsWalkable(start))
            {
                failureReason = $"起点不可通行: {start}.";
                return false;
            }

            if (!mapGrid.IsWalkable(target))
            {
                failureReason = $"终点不可通行: {target}.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static Vector3 GetWorldInput(Transform transformValue, Vector3 fallback)
        {
            return transformValue != null ? transformValue.position : fallback;
        }

        private void BuildWorldPathFromCells(MapGridManager mapGrid)
        {
            worldPath.Clear();
            foreach (Vector3Int cell in pathCells)
            {
                worldPath.Add(mapGrid.GetCellCenterWorld(cell));
            }
        }

        private void BuildCellsFromWorldPath(MapGridManager mapGrid)
        {
            pathCells.Clear();
            foreach (Vector3 point in worldPath)
            {
                pathCells.Add(mapGrid.WorldToCell(point));
            }
        }

        private void DrawPath(MapGridManager mapGrid)
        {
            Vector3 previousCenter = Vector3.zero;
            for (int i = 0; i < pathCells.Count; i++)
            {
                Vector3Int cell = pathCells[i];
                Vector3 center = mapGrid.GetCellCenterWorld(cell);
                DrawCell(mapGrid.CurrentGrid, cell, GetNodeFillColor(i, pathCells.Count), GetNodeOutlineColor(i, pathCells.Count));

                if (i > 0)
                {
                    Handles.color = new Color(pathColor.r, pathColor.g, pathColor.b, 1f);
                    Handles.DrawAAPolyLine(4f, previousCenter, center);
                }

                if (showNodeLabels)
                {
                    Handles.Label(center, i.ToString());
                }

                previousCenter = center;
            }
        }

        private void DrawSearchProcess(MapGridManager mapGrid)
        {
            if (!TryGetCurrentDebugStep(out MapPathfindingDebugStep step))
            {
                return;
            }

            foreach (Vector3Int closedCell in step.ClosedCells)
            {
                DrawCell(mapGrid.CurrentGrid, closedCell, new Color(0.35f, 0.35f, 0.35f, 0.28f), new Color(0.45f, 0.45f, 0.45f, 0.85f));
            }

            foreach (Vector3Int openCell in step.OpenCells)
            {
                DrawCell(mapGrid.CurrentGrid, openCell, new Color(1f, 0.82f, 0.12f, 0.34f), new Color(1f, 0.68f, 0.06f, 0.95f));
            }

            Handles.color = new Color(1f, 0.58f, 0.08f, 0.75f);
            foreach (KeyValuePair<Vector3Int, Vector3Int> pair in step.CameFrom)
            {
                Vector3 from = mapGrid.GetCellCenterWorld(pair.Value);
                Vector3 to = mapGrid.GetCellCenterWorld(pair.Key);
                Handles.DrawAAPolyLine(2f, from, to);
            }

            DrawCell(mapGrid.CurrentGrid, step.CurrentCell, new Color(0.92f, 0.16f, 1f, 0.48f), new Color(0.88f, 0.06f, 1f, 1f));

            if (showNodeLabels)
            {
                DrawSearchLabels(mapGrid, step);
            }
        }

        private void DrawSearchLabels(MapGridManager mapGrid, MapPathfindingDebugStep step)
        {
            HashSet<Vector3Int> labeledCells = new HashSet<Vector3Int>();
            DrawSearchLabel(mapGrid, step, step.CurrentCell, "cur", labeledCells);

            foreach (Vector3Int cell in step.OpenCells)
            {
                DrawSearchLabel(mapGrid, step, cell, "open", labeledCells);
            }

            foreach (Vector3Int cell in step.ClosedCells)
            {
                DrawSearchLabel(mapGrid, step, cell, "closed", labeledCells);
            }
        }

        private void DrawSearchLabel(
            MapGridManager mapGrid,
            MapPathfindingDebugStep step,
            Vector3Int cell,
            string state,
            HashSet<Vector3Int> labeledCells)
        {
            if (!labeledCells.Add(cell))
            {
                return;
            }

            string label = state;
            if (showSearchCosts && step.TryGetCosts(cell, out int gCost, out int hCost, out int fCost))
            {
                label = $"{state}\ng:{gCost} h:{hCost} f:{fCost}";
            }

            Handles.Label(mapGrid.GetCellCenterWorld(cell), label);
        }

        private static void DrawCell(Grid targetGrid, Vector3Int cell, Color fill, Color outline)
        {
            Vector3 bottomLeft = targetGrid.CellToWorld(cell);
            Vector3 bottomRight = targetGrid.CellToWorld(cell + Vector3Int.right);
            Vector3 topRight = targetGrid.CellToWorld(cell + Vector3Int.right + Vector3Int.up);
            Vector3 topLeft = targetGrid.CellToWorld(cell + Vector3Int.up);
            Vector3[] vertices = { bottomLeft, bottomRight, topRight, topLeft };
            Handles.DrawSolidRectangleWithOutline(vertices, fill, outline);
        }

        private Color GetNodeFillColor(int index, int count)
        {
            if (index == 0)
            {
                return new Color(0.1f, 0.9f, 0.25f, 0.38f);
            }

            if (index == count - 1)
            {
                return new Color(0.95f, 0.12f, 0.12f, 0.38f);
            }

            return new Color(pathColor.r, pathColor.g, pathColor.b, Mathf.Clamp01(pathColor.a));
        }

        private static Color GetNodeOutlineColor(int index, int count)
        {
            if (index == 0)
            {
                return new Color(0.05f, 0.8f, 0.2f, 1f);
            }

            if (index == count - 1)
            {
                return new Color(0.9f, 0.08f, 0.08f, 1f);
            }

            return new Color(0.12f, 0.55f, 1f, 0.95f);
        }

        private bool TryGetCurrentDebugStep(out MapPathfindingDebugStep step)
        {
            if (debugResult.Steps.Count == 0)
            {
                step = null;
                return false;
            }

            processStepIndex = Mathf.Clamp(processStepIndex, 0, debugResult.Steps.Count - 1);
            step = debugResult.Steps[processStepIndex];
            return true;
        }

        private void SetSuccess(string message)
        {
            lastSearchSucceeded = true;
            SetStatus(message);
            Debug.Log($"[MapPathfindingOdinTester] {message}");
            SceneView.RepaintAll();
        }

        private void SetFailure(string message)
        {
            pathCells.Clear();
            worldPath.Clear();
            lastSearchSucceeded = false;
            SetStatus(message);
            Debug.LogError($"[MapPathfindingOdinTester] {message}");
            SceneView.RepaintAll();
        }

        private void SetStatus(string message)
        {
            statusText = message;
            SceneView.RepaintAll();
        }
    }
}
#endif
