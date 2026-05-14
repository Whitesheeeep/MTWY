using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public static class GameDatabase
    {
        private static readonly Dictionary<Type, IGameSubDatabase> Databases = new Dictionary<Type, IGameSubDatabase>();

        public static void Register<TDatabase>(TDatabase database)
            where TDatabase : class, IGameSubDatabase
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            Type databaseType = typeof(TDatabase);
            if (Databases.ContainsKey(databaseType))
            {
                Debug.LogWarning($"[GameDatabase] Database already registered and will be replaced: {databaseType.Name}");
                Databases[databaseType].Clear();
            }

            Databases[databaseType] = database;
        }

        public static TDatabase Get<TDatabase>()
            where TDatabase : class, IGameSubDatabase
        {
            if (TryGet(out TDatabase database))
            {
                return database;
            }

            throw new InvalidOperationException($"[GameDatabase] Database is not registered: {typeof(TDatabase).Name}");
        }

        public static bool TryGet<TDatabase>(out TDatabase database)
            where TDatabase : class, IGameSubDatabase
        {
            if (Databases.TryGetValue(typeof(TDatabase), out IGameSubDatabase value) && value is TDatabase typed)
            {
                database = typed;
                return true;
            }

            database = null;
            return false;
        }

        public static void Clear()
        {
            foreach (IGameSubDatabase database in Databases.Values)
            {
                database?.Clear();
            }

            Databases.Clear();
        }
    }
}
