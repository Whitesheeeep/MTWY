using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    [CreateAssetMenu(fileName = "GameDatabaseRegisterModule", menuName = "GameData/Database/Register Module", order = 0)]
    public sealed class GameDatabaseRegisterModule : CompositeConfigRegisterNode
    {
        public override void Register()
        {
            // 总模块只负责建立干净的注册上下文，具体数据库由子节点创建并注册。
            GameDatabase.Clear();
            base.Register();
        }
    }
}
