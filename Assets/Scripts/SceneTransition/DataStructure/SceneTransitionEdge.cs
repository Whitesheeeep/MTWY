using System;
using UnityEngine;

namespace GameData.SceneTransition
{
    /// <summary>
    /// A directed scene transition edge between two scene points.
    /// </summary>
    [Serializable]
    public struct SceneTransitionEdge
    {
        public string edgeId;
        // public string displayName;

        public SceneTransitionPoint_SO fromPoint;
        public SceneTransitionPoint_SO toPoint;

        public int cost;
        public bool resetRigidbodyVelocity;
        public bool applySpawnRotation;
        public Vector3 spawnEulerAngles;
    }
}
