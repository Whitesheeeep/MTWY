using System.Collections.Generic;

namespace GameData
{
    public interface IItemDatabase : IGameSubDatabase
    {
        bool TryGet(int id, out ItemData item);
        ItemData Get(int id);
        IReadOnlyList<ItemData> GetAll();
    }
}
