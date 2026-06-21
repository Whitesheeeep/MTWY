using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 作物配置列表。当前只提供数据入口，不承载运行时成长逻辑。
    /// </summary>
    [CreateAssetMenu(fileName = "CropDataList", menuName = "GameData/Crop Data List", order = 0)]
    public class CropDataList_SO : ScriptableObject
    {
        public List<CropData> Crops = new List<CropData>();
    }
}
