namespace ZFrameWork
{
    public abstract class PopupBase<TData> : UIBase
    {
        public TData Data { get; private set; }

        public void SetData(TData data)
        {
            Data = data;
        }
    }
}
