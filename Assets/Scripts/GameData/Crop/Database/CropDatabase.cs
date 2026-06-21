using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public sealed class CropDatabase : ICropDatabase
    {
        private readonly Dictionary<int, CropData> cropMap = new Dictionary<int, CropData>();
        private readonly Dictionary<int, CropData> cropBySeedItemId = new Dictionary<int, CropData>();
        private readonly List<CropData> crops = new List<CropData>();

        public CropDatabase(CropDataList_SO dataList)
        {
            Initialize(dataList);
        }

        public bool TryGet(int id, out CropData crop)
        {
            return cropMap.TryGetValue(id, out crop);
        }

        public CropData Get(int id)
        {
            if (TryGet(id, out CropData crop))
            {
                return crop;
            }

            throw new KeyNotFoundException($"[CropDatabase] Crop id not found: {id}");
        }

        public bool TryGetBySeedItemId(int seedItemId, out CropData crop)
        {
            return cropBySeedItemId.TryGetValue(seedItemId, out crop);
        }

        public IReadOnlyList<CropData> GetAll()
        {
            return crops;
        }

        public void Clear()
        {
            cropMap.Clear();
            cropBySeedItemId.Clear();
            crops.Clear();
        }

        private void Initialize(CropDataList_SO dataList)
        {
            Clear();

            if (dataList == null)
            {
                Debug.LogError("[CropDatabase] CropDataList_SO is null.");
                return;
            }

            if (dataList.Crops == null)
            {
                Debug.LogWarning($"[CropDatabase] Crop list is null: {dataList.name}");
                return;
            }

            foreach (CropData crop in dataList.Crops)
            {
                if (crop == null)
                {
                    Debug.LogWarning($"[CropDatabase] Null crop skipped in {dataList.name}.");
                    continue;
                }

                if (crop.Id <= 0)
                {
                    Debug.LogWarning($"[CropDatabase] Invalid crop id skipped: {crop.Id}");
                    continue;
                }

                if (cropMap.ContainsKey(crop.Id))
                {
                    Debug.LogError($"[CropDatabase] Duplicate crop id skipped: {crop.Id}");
                    continue;
                }

                cropMap.Add(crop.Id, crop);
                crops.Add(crop);
                RegisterSeedLookup(crop);
                ValidateOptionalData(crop);
            }
        }

        private void RegisterSeedLookup(CropData crop)
        {
            if (crop.SeedItemId <= 0)
            {
                Debug.LogWarning($"[CropDatabase] Crop seed item id is invalid and will not be indexed. cropId={crop.Id}, seedItemId={crop.SeedItemId}");
                return;
            }

            if (cropBySeedItemId.ContainsKey(crop.SeedItemId))
            {
                Debug.LogError($"[CropDatabase] Duplicate seed item id skipped. cropId={crop.Id}, seedItemId={crop.SeedItemId}");
                return;
            }

            cropBySeedItemId.Add(crop.SeedItemId, crop);
        }

        private static void ValidateOptionalData(CropData crop)
        {
            if (crop.GrowthStages == null || crop.GrowthStages.Count == 0)
            {
                Debug.LogWarning($"[CropDatabase] Crop growth stages are empty. cropId={crop.Id}");
            }

            if (crop.CanRegrow &&
                (crop.GrowthStages == null ||
                 crop.RegrowStageIndex < 0 ||
                 crop.RegrowStageIndex >= crop.GrowthStages.Count))
            {
                Debug.LogWarning($"[CropDatabase] Crop regrow stage index is out of range. cropId={crop.Id}, regrowStageIndex={crop.RegrowStageIndex}");
            }
        }
    }
}
