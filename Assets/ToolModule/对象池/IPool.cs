namespace ZFrameWork
{
    /// <summary>
    /// 对象池对外接口，定义借出和归还行为。
    /// </summary>
    public interface IPool<T> where T : class
    {
        /// <summary>
        /// 从池中获取一个对象。池中有空闲则直接返回，池空则创建新对象。
        /// </summary>
        T Get();

        /// <summary>
        /// 将对象归还到池中。
        /// </summary>
        void Add(T obj);

        /// <summary>当前池中空闲对象数量。</summary>
        int Count { get; }

        /// <summary>池的容量上限。</summary>
        int Capacity { get; }
    }
}
