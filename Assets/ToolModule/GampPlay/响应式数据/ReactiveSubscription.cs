using System;

namespace ZFrameWork
{
    internal static class ReactiveSubscription
    {
        internal static readonly Action Empty = () => { };

        internal static Action Create(Action unsubscribe)
        {
            var invoked = false;

            return () =>
            {
                if (invoked) return;

                invoked = true;
                unsubscribe?.Invoke();
                unsubscribe = null;
            };
        }
    }
}
