# LinkedEvent Design

## Goal

Add event-oriented wrappers alongside `LinkedAction` so callers can subscribe with standard C# `+=` and `-=` syntax while retaining the framework's duplicate prevention and mutation-safe invocation behavior.

## API

Add two public sealed types in namespace `ZFrameWork`:

```csharp
public sealed class LinkedEvent
{
    public int Count { get; }
    public event Action Invoked;
    public void Invoke();
    public void Clear();
}

public sealed class LinkedEvent<T>
{
    public int Count { get; }
    public event Action<T> Invoked;
    public void Invoke(T arg);
    public void Clear();
}
```

## Implementation

Each wrapper owns a `LinkedAction` or `LinkedAction<T>`. Custom event accessors delegate additions to `Subscribe` and removals to `Unsubscribe`; `Invoke`, `Clear`, and `Count` delegate to the same instance. Do not duplicate linked-list storage or traversal code.

The wrappers inherit these existing semantics:

- Null subscriptions are ignored.
- Duplicate subscriptions are ignored.
- Removing an absent listener is harmless.
- Listeners may add or remove listeners during invocation without corrupting traversal or invoking one listener twice in the same pass.

## Scope

Do not modify `LinkedAction`, `EventCenter`, or existing call sites. Do not add multi-argument variants, priorities, exception swallowing, asynchronous invocation, or Unity serialization.

## Verification

Use a temporary compile harness to establish that `LinkedEvent` does not yet exist, then test zero-argument and generic event subscription, duplicate prevention, removal, clearing, argument propagation, and self-removal during invocation. Finally build `Assembly-CSharp.csproj` and remove the temporary harness.
