# Replace Module Exceptions with ZLog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all 12 explicit module `throw` statements with `ZLog.LogError` followed by a safe immediate return.

**Architecture:** Preserve public APIs and existing messages. `void` methods return immediately, value-returning methods return `default`, and failed Addressables completion handlers stop before invoking success callbacks.

**Tech Stack:** Unity 2022.3, C#, `ZLog`, temporary .NET 10 compile harness

## Global Constraints

- Use `ZLog.LogError`, not a new `ZLog.Error` API.
- Replace only explicit project-owned `throw` statements under `Assets`.
- Do not catch exceptions raised by framework or external-library calls.
- Keep existing error messages and method signatures.
- Remove temporary tests after verification.
- The workspace is not a Git repository, so commit steps are not applicable.

---

### Task 1: Add failing behavior coverage

**Files:**
- Test temporarily: `Temp/ExceptionBehaviorTests/ExceptionBehaviorTests.csproj`
- Test temporarily: `Temp/ExceptionBehaviorTests/UnityStubs.cs`
- Test temporarily: `Temp/ExceptionBehaviorTests/Program.cs`

**Interfaces:**
- Consumes: `StateBlackBoard.GetData<T>`, `UnityTimer.StartRepeatTimer`, and `UnityTimer.StartOnceTimer`.
- Produces: executable assertions for default returns, early returns, and six Editor error logs.

- [x] **Step 1: Create a harness linking the real module sources**

Link `ZLog.cs`, `StateBlackBoard.cs`, and `Timer.cs`. Stub only `UnityEngine.Debug`, `WaitForSecondsRealtime`, and `PublicMono.StartCoroutine`.

`ExceptionBehaviorTests.csproj` links those three production files into a `net10.0` console executable and uses a local `NuGet.Config` with cleared package sources.

```csharp
namespace UnityEngine;

public static class Debug
{
    public static int ErrorCalls { get; private set; }
    public static void Log(object message) { }
    public static void LogWarning(object message) { }
    public static void LogError(object message) => ErrorCalls++;
}

public sealed class WaitForSecondsRealtime
{
    public WaitForSecondsRealtime(float seconds) { }
}
```

```csharp
namespace ZFrameWork;

public sealed class PublicMono
{
    public static PublicMono Instance { get; } = new();
    public static int StartCoroutineCalls { get; private set; }
    public void StartCoroutine(System.Collections.IEnumerator routine) => StartCoroutineCalls++;
}
```

```csharp
var board = new ZFrameWork.StateBlackBoard();
int missingInt = board.GetData<int>("missing");
string missingString = board.GetData<string>("missing");

var timer = new ZFrameWork.UnityTimer();
timer.StartRepeatTimer(0, () => { });
timer.StartRepeatTimer(1, null);
timer.StartOnceTimer(0, () => { });
timer.StartOnceTimer(1, null);

bool passed = missingInt == 0 && missingString is null &&
              UnityEngine.Debug.ErrorCalls == 6 &&
              ZFrameWork.PublicMono.StartCoroutineCalls == 0;
```

- [x] **Step 2: Run RED with `UNITY_EDITOR` defined**

Run: `dotnet run --project Temp/ExceptionBehaviorTests/ExceptionBehaviorTests.csproj -p:DefineConstants=UNITY_EDITOR`

Expected: the current `StateBlackBoard.GetData` throws `KeyNotFoundException`, so the harness exits nonzero.

### Task 2: Replace timer and blackboard exceptions

**Files:**
- Modify: `Assets/ToolModule/时间管理/Timer.cs`
- Modify: `Assets/ToolModule/状态机/StateBlackBoard.cs`

**Interfaces:**
- Produces: timer validation logs and returns before reset/coroutine startup.
- Produces: missing blackboard reads log and return `default`.

- [x] **Step 1: Replace each timer throw**

Use the existing exception text in `ZLog.LogError(...)`, followed by `return` in the same validation block.

- [x] **Step 2: Replace both blackboard throws**

Use `ZLog.LogError(...)`, followed by `return default` in each missing-data branch.

- [x] **Step 3: Run GREEN behavior test**

Run the Task 1 harness with `UNITY_EDITOR`; expect `PASS`, six error calls, default values, and zero coroutine starts.

### Task 3: Replace JSON and Addressables exceptions

**Files:**
- Modify: `Assets/ToolModule/数据持久化/JsonManager.cs`
- Modify: `Assets/ToolModule/可寻址/AddressableManager.cs`

**Interfaces:**
- Produces: `LoadJson<T>` returns `default` for missing directories/files; `SaveData<T>` returns for a missing directory.
- Produces: async Addressables failures log and exit without success callbacks; sync failures log and return `default`.

- [x] **Step 1: Establish the residual-throw RED check**

Run: `rg -n --glob '*.cs' '\bthrow\b' Assets`

Expected: all remaining JSON and Addressables throws are reported.

- [x] **Step 2: Apply safe return behavior**

For each current exception branch, call `ZLog.LogError` with the same message, then use `return`, `return default`, or lambda-local `return` according to the method contract.

- [x] **Step 3: Verify no explicit throws remain**

Run the Task 1 harness again and require `rg` to find zero `throw` tokens under `Assets`.

- [x] **Step 4: Build the full Unity assembly**

Run: `dotnet build Assembly-CSharp.csproj --no-restore --nologo`

Expected: build succeeds with zero errors.

- [x] **Step 5: Clean temporary tests**

Resolve and verify `Temp/ExceptionBehaviorTests` is under the project `Temp` directory, then delete only that directory.
