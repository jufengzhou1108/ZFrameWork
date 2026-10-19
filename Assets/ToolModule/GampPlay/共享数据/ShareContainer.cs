using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    public sealed class ShareContainer
    {
        private readonly Dictionary<string, object> _items = new();

        public void Set<T>(string key, T model) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，key 不能为空。");
                return;
            }
            if (model == null)
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，不能放入空引用，key：{key}");
                return;
            }
            if (_items.ContainsKey(key))
            {
                ZLog.LogError($"[{GetType().Name}] 放入失败，key 已被占用：{key}（登记类型 {_items[key].GetType().Name}）。"
                    + "若要替换，请先 Delete 再 Set。");
                return;
            }

            _items.Add(key, model);
        }

        public T Get<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                ZLog.LogError($"[{GetType().Name}] 获取失败，key 不能为空。");
                return null;
            }
            if (!_items.TryGetValue(key, out object stored))
            {
                ZLog.LogError($"[{GetType().Name}] 获取失败，key 未登记：{key}");
                return null;
            }
            if (stored is T typed)
                return typed;

            // 走到这里即 is 转换失败（Set 拒绝空引用，所以取到的值不会真的是 null）：
            // 是使用者把 key 和类型的对应关系配错了
            ZLog.LogError($"[{GetType().Name}] 获取失败，登记类型与请求类型不符，key：{key}，"
                + $"登记类型 {stored.GetType().Name}，请求类型 {typeof(T).Name}。");
            return null;
        }

        public void Delete(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                ZLog.LogError($"[{GetType().Name}] 删除失败，key 不能为空。");
                return;
            }
            if (!_items.Remove(key))
            {
                // 删不存在的项意味着调用方的前提被破坏（重复删除，或从未登记），按约定报错提醒
                ZLog.LogError($"[{GetType().Name}] 删除失败，key 未登记：{key}");
            }
        }
    }
}
