using System.Collections.Generic;

namespace GameData
{
    public interface ICropDatabase : IGameSubDatabase
    {
        bool TryGet(int id, out CropData crop);
        CropData Get(int id);
        bool TryGetBySeedItemId(int seedItemId, out CropData crop);
        IReadOnlyList<CropData> GetAll();
    }
}
