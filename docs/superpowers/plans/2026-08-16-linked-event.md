# LinkedEvent Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add zero-argument and one-argument event wrappers with standard C# subscription syntax and existing `LinkedAction` semantics.

**Architecture:** Implement `LinkedEvent` and `LinkedEvent<T>` through composition over `LinkedAction` and `LinkedAction<T>`. Custom `Invoked` event accessors route `+=` and `-=` to the existing subscription implementation.

**Tech Stack:** C#, .NET events, Unity 2022.3, temporary .NET 10 harness

## Global Constraints

- Add only `Assets/ToolModule/封装/委托封装/LinkedEvent.cs` as production code.
- Do not modify `LinkedAction`, `EventCenter`, or existing call sites.
- Preserve duplicate prevention and mutation-safe invocation through composition.
- Provide `Invoked`, `Count`, `Invoke`, and `Clear` on both wrapper types.
- Remove temporary tests after verification.
- The workspace is not a Git repository, so commit steps are not applicable.

---

### Task 1: Add LinkedEvent wrappers

**Files:**
- Create: `Assets/ToolModule/封装/委托封装/LinkedEvent.cs`
- Test temporarily: `Temp/LinkedEventTests/LinkedEventTests.csproj`
- Test temporarily: `Temp/LinkedEventTests/Program.cs`
- Test temporarily: `Temp/LinkedEventTests/NuGet.Config`

**Interfaces:**
- Produces: `sealed class LinkedEvent` with `event Action Invoked`, `int Count`, `void Invoke()`, and `void Clear()`.
- Produces: `sealed class LinkedEvent<T>` with `event Action<T> Invoked`, `int Count`, `void Invoke(T arg)`, and `void Clear()`.

- [x] **Step 1: Write the failing executable test**

The test project includes all `Linked*.cs` files from the production directory. `Program.cs` verifies duplicate prevention, removal, clearing, argument propagation, and self-removal during invocation:

```csharp
using System;
using ZFrameWork;

var simpleEvent = new LinkedEvent();
int count = 0;
Action callback = () => count++;
simpleEvent.Invoked += callback;
simpleEvent.Invoked += callback;
simpleEvent.Invoke();
bool simplePassed = count == 1 && simpleEvent.Count == 1;
simpleEvent.Invoked -= callback;
simplePassed &= simpleEvent.Count == 0;

var typedEvent = new LinkedEvent<int>();
int sum = 0;
Action<int> selfRemoving = null;
selfRemoving = value =>
{
    sum += value;
    typedEvent.Invoked -= selfRemoving;
};
Action<int> remaining = value => sum += value * 10;
typedEvent.Invoked += selfRemoving;
typedEvent.Invoked += remaining;
typedEvent.Invoke(2);
typedEvent.Invoke(2);
bool typedPassed = sum == 42 && typedEvent.Count == 1;
typedEvent.Clear();
typedPassed &= typedEvent.Count == 0;

Console.WriteLine(simplePassed && typedPassed ? "PASS" : "FAIL");
return simplePassed && typedPassed ? 0 : 1;
```

- [x] **Step 2: Run RED**

Run: `dotnet run --project Temp/LinkedEventTests/LinkedEventTests.csproj`

Expected: compilation fails because `LinkedEvent` and `LinkedEvent<T>` do not exist.

- [x] **Step 3: Add the minimal production implementation**

```csharp
using System;

namespace ZFrameWork
{
    public sealed class LinkedEvent
    {
        private readonly LinkedAction _callbacks = new();
        public int Count => _callbacks.Count;
        public event Action Invoked
        {
            add => _callbacks.Subscribe(value);
            remove => _callbacks.Unsubscribe(value);
        }
        public void Invoke() => _callbacks.Invoke();
        public void Clear() => _callbacks.Clear();
    }

    public sealed class LinkedEvent<T>
    {
        private readonly LinkedAction<T> _callbacks = new();
        public int Count => _callbacks.Count;
        public event Action<T> Invoked
        {
            add => _callbacks.Subscribe(value);
            remove => _callbacks.Unsubscribe(value);
        }
        public void Invoke(T arg) => _callbacks.Invoke(arg);
        public void Clear() => _callbacks.Clear();
    }
}
```

- [x] **Step 4: Run GREEN and full build**

Run the temporary test and require `PASS`, then run `dotnet build Assembly-CSharp.csproj --no-restore --nologo` and require zero errors.

- [x] **Step 5: Remove temporary tests**

Resolve and verify `Temp/LinkedEventTests` is under the project `Temp` directory, then delete only that directory.
