using System;
using System.Collections.Generic;

namespace GameData
{
    /// <summary>
    /// 对话运行时服务表，由 Runner Host 或测试器负责注册外部服务引用。
    /// </summary>
    public sealed class DialogueServices : IDialogueServices
    {
        #region 字段
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        #endregion

        #region 服务维护
        /// <summary>
        /// 注册指定类型的服务实例。注册 null 时会移除该服务。
        /// </summary>
        /// <typeparam name="T">服务接口或服务类型。</typeparam>
        /// <param name="service">服务实例。</param>
        public void Register<T>(T service)
        {
            Type serviceType = typeof(T);
            if (service is null)
            {
                services.Remove(serviceType);
                return;
            }

            services[serviceType] = service;
        }

        /// <summary>
        /// 移除指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务接口或服务类型。</typeparam>
        public void Remove<T>()
        {
            services.Remove(typeof(T));
        }

        /// <summary>
        /// 清空当前服务表中所有服务引用。
        /// </summary>
        public void Clear()
        {
            services.Clear();
        }
        #endregion

        #region 服务查询
        /// <inheritdoc />
        public bool TryGet<T>(out T service)
        {
            if (services.TryGetValue(typeof(T), out object value) && value is T typedValue)
            {
                service = typedValue;
                return true;
            }

            service = default;
            return false;
        }
        #endregion
    }
}
