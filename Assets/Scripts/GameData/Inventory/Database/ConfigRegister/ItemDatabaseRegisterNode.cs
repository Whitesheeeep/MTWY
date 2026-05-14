using UnityEngine;
using WS_Modules.ConfigInstaller;
using WS_Modules.LogModule;

namespace GameData
{
    [CreateAssetMenu(fileName = "ItemDatabaseRegisterNode", menuName = "GameData/Database/Item Register Node", order = 1)]
    public sealed class ItemDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        [SerializeField] private ItemDataList_SO itemDataList;

        public override void Register()
        {
            if (itemDataList == null)
            {
                Debug.LogError("[ItemDatabaseRegisterNode] ItemDataList_SO is not assigned.");
                return;
            }

            IItemDatabase itemDatabase = new ItemDatabase(itemDataList);
            GameDatabase.Register<IItemDatabase>(itemDatabase);
            Debug.Log($"[ItemDatabaseRegisterNode] 注册 ItemDatabase， 一共注册 ItemDatabase: {itemDataList.items?.Count ?? 0} items.");
        }
    }
}
