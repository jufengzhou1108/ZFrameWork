using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    // 系统上下文：系统在组合阶段把自己的服务登记进来，内部按类型自取。
    // 只放服务和查询接口，不放实体和业务数据；上下文归创建它的系统所有，生命周期跟系统走。
    public sealed class SystemContext
    {
        private readonly Dictionary<Type, object> _items = new();

        // T 取调用处的静态类型：想按接口登记，显式写 Set<IDamageService>(impl)，或用接口类型的变量。
        public void Set<T>(T model) where T : class
        {
            Set(typeof(T), model);
        }

        public void Set(Type key, object model)
        {
            if (key == null)
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，key 不能为空。");
                return;
            }
            if (model == null)
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，不能放入空引用，key：{key.Name}");
                return;
            }
            if (!key.IsInstanceOfType(model))
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，登记类型与 key 不符，key：{key.Name}，"
                    + $"实际类型 {model.GetType().Name}。");
                return;
            }
            if (_items.ContainsKey(key))
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，key 已被占用：{key.Name}（登记类型 {_items[key].GetType().Name}）。"
                    + "若要替换，请先 Delete 再 Set。");
                return;
            }

            _items.Add(key, model);
        }

        public T Get<T>() where T : class
        {
            return Get(typeof(T)) as T;
        }

        public object Get(Type key)
        {
            if (key == null)
            {
                ZLog.LogError($"[{GetType().Name}] 获取失败，key 不能为空。");
                return null;
            }
            if (!_items.TryGetValue(key, out object stored))
            {
                ZLog.LogError($"[{GetType().Name}] 获取失败，key 未登记：{key.Name}");
                return null;
            }
            return stored;
        }

        public void Delete<T>() where T : class
        {
            Delete(typeof(T));
        }

        public void Delete(Type key)
        {
            if (key == null)
            {
                ZLog.LogError($"[{GetType().Name}] 删除失败，key 不能为空。");
                return;
            }
            if (!_items.Remove(key))
            {
                ZLog.LogError($"[{GetType().Name}] 删除失败，key 未登记：{key.Name}");
            }
        }
    }
}
