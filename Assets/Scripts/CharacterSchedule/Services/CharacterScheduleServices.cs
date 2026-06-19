using System;
using System.Collections.Generic;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 日程服务容器。由 CharacterScheduleManager 持有，Condition 通过接口按需读取服务。
    /// </summary>
    public sealed class CharacterScheduleServices : ICharacterScheduleServices
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// 注册或替换服务。传入 null 会移除该类型服务。
        /// </summary>
        public void Register<T>(T service)
        {
            Type type = typeof(T);
            if (service == null)
            {
                services.Remove(type);
                return;
            }

            services[type] = service;
        }

        /// <summary>
        /// 清空所有服务。
        /// </summary>
        public void Clear()
        {
            services.Clear();
        }

        /// <summary>
        /// 按类型查询服务。
        /// </summary>
        public bool TryGet<T>(out T service)
        {
            if (services.TryGetValue(typeof(T), out object value) && value is T typed)
            {
                service = typed;
                return true;
            }

            service = default;
            return false;
        }
    }
}
