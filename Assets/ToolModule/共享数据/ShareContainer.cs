using System;
using System.Collections.Generic;

namespace ZFrameWork
{
    /// <summary>
    /// 类共享容器：让业务类（UI 或其它业务类）把自建的 Model 分享给其他业务类。
    /// 语义上只负责分享，不决定 Model 的生命周期——Model 何时该消失，
    /// 由负责它生命周期的类决定，容器只提供 Set / Delete / Get 三个操作。
    ///
    /// 约定：
    /// - 主线程专用，不做线程安全保障；
    /// - 强引用持有：登记期间目标必然有效，取到的调用方不需要自己保活；
    ///   正因如此，漏 Delete 等于容器永久持有它，删除的责任在"负责该 Model 生命周期的类"；
    /// - 谁都可以 Delete：Model 与业务类的生命周期并不总是一致，
    ///   删除只解除分享、不销毁对象，已取走引用的调用方不受影响。
    /// </summary>
    public sealed class ShareContainer
    {
        private readonly Dictionary<string, object> _items = new();

        /// <summary>放入共享 Model。重复、传 null、空 key 都报错且不改变已有登记。</summary>
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

        /// <summary>获取共享 Model。无 key、空 key、登记类型与请求类型不符都报错并返回 null。</summary>
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

        /// <summary>取消分享：删除对应的字典项。无 key、空 key 都报错。</summary>
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

        /// <summary>仅供测试：清空索引。只删字典，不触碰任何已分享对象。</summary>
        internal void ClearForTest()
        {
            _items.Clear();
        }
    }
}
