using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using WorldItems;

public class TempWorldItemDic : MonoBehaviour
{
    [ReadOnly]
    public Dictionary<string, WorldItems.WorldItemSceneBucket> WorldItemRecords;

    private void Start()
    {
        WorldItemRecords = WorldItemManager.BucketsByMapId;
    }

    [Button]
    private void DebugPrintAllRecords()
    {
        foreach (var kvp in WorldItemRecords)
        {
            string mapId = kvp.Key;
            WorldItemSceneBucket bucket = kvp.Value;
            Debug.Log($"Map ID: {mapId}, Item Count: {bucket.Count}");
            foreach (var record in bucket.Records)
            {
                Debug.Log($"  Instance ID: {record.InstanceId}, Item ID: {record.ItemId}, Count: {record.Count}, Position: {record.Position}");
            }
        }
    }
}
