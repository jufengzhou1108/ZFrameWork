namespace ZFrameWork
{
    public sealed class AddressablesFactory : IFactory
    {
        private AddressablesLoadManager _manager;

        public AddressablesLoadManager Manager => _manager ??= new AddressablesLoadManager();

        public IResourceGroup Create()
        {
            return new AddressablesGroup(Manager);
        }
    }
}
