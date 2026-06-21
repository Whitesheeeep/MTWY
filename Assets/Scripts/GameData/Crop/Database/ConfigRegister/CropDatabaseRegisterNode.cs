using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    [CreateAssetMenu(fileName = "CropDatabaseRegisterNode", menuName = "GameData/Database/Crop Register Node", order = 4)]
    public sealed class CropDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        [SerializeField] private CropDataList_SO cropDataList;

        public override void Register()
        {
            if (cropDataList == null)
            {
                Debug.LogError("[CropDatabaseRegisterNode] CropDataList_SO is not assigned.");
                return;
            }

            ICropDatabase cropDatabase = new CropDatabase(cropDataList);
            GameDatabase.Register<ICropDatabase>(cropDatabase);
            Debug.Log($"[CropDatabaseRegisterNode] Registered CropDatabase: {cropDataList.Crops?.Count ?? 0} crops.");
        }
    }
}
