using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 当前场景中角色的实体表现，只负责格子路径的可见移动和到达回报。
    /// 角色的逻辑状态仍由 CharacterScheduleManager 维护。
    /// </summary>
    public sealed class CharacterAgent : MonoBehaviour
    {
        private const float AgentWorldZ = 0f;

        /// <summary>
        /// 可选 Rigidbody2D。存在时使用 MovePosition，缺省时直接移动 Transform。
        /// </summary>
        [SerializeField] private Rigidbody2D agentRigidbody2D;

        /// <summary>
        /// 判断到达目标 cell 世界中心的距离阈值。
        /// </summary>
        [SerializeField] private float arriveThreshold = 0.02f;

        private readonly List<Vector3Int> pathCells = new List<Vector3Int>();
        private string characterId;
        private int pathIndex;
        private float moveSpeed;
        private bool isMoving;

        /// <summary>
        /// 当前绑定的角色 ID。
        /// </summary>
        public string CharacterId => characterId;

        /// <summary>
        /// 当前 Agent 是否正在执行路径移动。
        /// </summary>
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (agentRigidbody2D == null)
            {
                agentRigidbody2D = GetComponent<Rigidbody2D>();
            }
        }

        private void FixedUpdate()
        {
            if (!isMoving)
            {
                return;
            }

            if (pathIndex < 0 || pathIndex >= pathCells.Count)
            {
                CompleteMove();
                return;
            }

            if (!MapGridManager.Instance.HasCurrentGrid)
            {
                StopMove();
                return;
            }

            Vector3 targetWorld = NormalizeAgentZ(MapGridManager.Instance.GetCellCenterWorld(pathCells[pathIndex]));
            Vector3 currentWorld = NormalizeAgentZ(transform.position);
            Vector3 nextWorld = Vector3.MoveTowards(currentWorld, targetWorld, moveSpeed * Time.fixedDeltaTime);

            if (agentRigidbody2D != null)
            {
                agentRigidbody2D.MovePosition(new Vector2(nextWorld.x, nextWorld.y));
                NormalizeTransformZ();
            }
            else
            {
                transform.position = nextWorld;
            }

            if (Vector2.Distance(nextWorld, targetWorld) > arriveThreshold)
            {
                return;
            }

            SetPosition(targetWorld);
            CharacterScheduleManager.Instance.ReportAgentReachedCell(characterId, pathCells[pathIndex]);
            pathIndex++;

            if (pathIndex >= pathCells.Count)
            {
                CompleteMove();
            }
        }

        /// <summary>
        /// 绑定角色 ID。Agent 可被对象池复用，因此生成后需要重新绑定。
        /// </summary>
        public void Bind(string newCharacterId)
        {
            characterId = newCharacterId;
        }

        /// <summary>
        /// 立即停在指定 cell 的世界中心。
        /// </summary>
        public void SnapToCell(Vector3Int cell)
        {
            StopMove();
            if (!MapGridManager.Instance.HasCurrentGrid)
            {
                return;
            }

            SetPosition(MapGridManager.Instance.GetCellCenterWorld(cell));
        }

        /// <summary>
        /// 沿一组 cell 执行可见移动。cells 不应包含当前所在格。
        /// </summary>
        public void MoveAlongCells(IReadOnlyList<Vector3Int> cells, float speed)
        {
            pathCells.Clear();
            if (cells == null || cells.Count == 0 || speed <= 0f)
            {
                StopMove();
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                pathCells.Add(cells[i]);
            }

            pathIndex = 0;
            moveSpeed = speed;
            isMoving = true;
        }

        /// <summary>
        /// 停止当前移动并清空路径。
        /// </summary>
        public void StopMove()
        {
            pathCells.Clear();
            pathIndex = 0;
            isMoving = false;
        }

        private void CompleteMove()
        {
            StopMove();
            CharacterScheduleManager.Instance.ReportMoveArrived(characterId);
        }

        private void SetPosition(Vector3 position)
        {
            position = NormalizeAgentZ(position);
            if (agentRigidbody2D != null)
            {
                agentRigidbody2D.position = new Vector2(position.x, position.y);
                transform.position = position;
            }
            else
            {
                transform.position = position;
            }
        }

        private static Vector3 NormalizeAgentZ(Vector3 position)
        {
            position.z = AgentWorldZ;
            return position;
        }

        private void NormalizeTransformZ()
        {
            Vector3 position = transform.position;
            if (Mathf.Approximately(position.z, AgentWorldZ))
            {
                return;
            }

            position.z = AgentWorldZ;
            transform.position = position;
        }
    }
}
