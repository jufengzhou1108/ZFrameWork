namespace ZFrameWork
{
    public static class ResourceGroupFactory
    {
        private static IFactory _factory;

        public static IFactory Factory
        {
            get
            {
                EnsureDefault();
                return _factory;
            }
        }

        public static IResourceGroup Create()
        {
            EnsureDefault();
            return _factory.Create();
        }

        public static void SetFactory(IFactory factory)
        {
            _factory = factory ?? new AddressablesFactory();
        }

        private static void EnsureDefault()
        {
            if (_factory == null)
                SetFactory(new AddressablesFactory());
        }
    }
}
