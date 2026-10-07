namespace ZFrameWork
{
    /// <summary>
    /// 弹窗基类。TData 为弹窗专属数据类型，一般声明为弹窗类的嵌套 Data 结构体。
    /// 数据在加载完成、Open 之前由管理器通过 SetData 注入，OnOpen/OnShow 内即可使用。
    /// </summary>
    public abstract class PopupBase<TData> : UIBase
    {
        /// <summary>当前弹窗数据，每次 Push 重新注入。
        /// 注意：子类嵌套的 Data 类型会遮蔽本属性的同名简单名，子类内部引用需写 base.Data。</summary>
        public TData Data { get; private set; }

        public void SetData(TData data)
        {
            Data = data;
        }
    }
}
