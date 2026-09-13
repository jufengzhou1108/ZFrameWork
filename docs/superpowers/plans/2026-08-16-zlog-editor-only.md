# ZLog Editor-Only Logging Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `ZLog.Log`, `ZLog.LogWarning`, and `ZLog.LogError` so Editor calls forward to Unity logging while Player builds omit calls and argument evaluation.

**Architecture:** Convert the existing empty `ZLog` component into a static facade. Apply `Conditional("UNITY_EDITOR")` at each public API and guard the internal Unity calls with `#if UNITY_EDITOR`; migrate existing direct logging calls to the facade.

**Tech Stack:** Unity 2022.3, C#, `System.Diagnostics.ConditionalAttribute`, temporary .NET compile harness

## Global Constraints

- Keep the public class name `ZLog` and use `object message` parameters.
- Provide only `Log`, `LogWarning`, and `LogError`; add no logging configuration or external dependencies.
- Preserve normal Editor logging and remove Player call-site argument evaluation.
- Keep test harness files under `Temp/ZLogCompileTests` and remove them after verification.
- The workspace is not a Git repository, so commit steps are not applicable.

---

### Task 1: Implement and prove conditional logging

**Files:**
- Modify: `Assets/ToolModule/封装/Log封装/ZLog.cs`
- Test temporarily: `Temp/ZLogCompileTests/ZLogCompileTests.csproj`
- Test temporarily: `Temp/ZLogCompileTests/UnityEngineStub.cs`
- Test temporarily: `Temp/ZLogCompileTests/Program.cs`

**Interfaces:**
- Produces: `static void ZLog.Log(object message)`
- Produces: `static void ZLog.LogWarning(object message)`
- Produces: `static void ZLog.LogError(object message)`

- [x] **Step 1: Write a compile harness that calls all three APIs**

The harness links the real `ZLog.cs`, supplies a minimal `UnityEngine.Debug` test double, calls every API with a side-effecting `Probe()`, and returns failure unless Editor compilation evaluates three arguments and Player compilation evaluates none.

`ZLogCompileTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\..\Assets\ToolModule\封装\Log封装\ZLog.cs" Link="ZLog.cs" />
  </ItemGroup>
</Project>
```

`UnityEngineStub.cs`:

```csharp
namespace UnityEngine;

public class MonoBehaviour { }

public static class Debug
{
    public static int LogCalls { get; private set; }
    public static int WarningCalls { get; private set; }
    public static int ErrorCalls { get; private set; }

    public static void Log(object message) => LogCalls++;
    public static void LogWarning(object message) => WarningCalls++;
    public static void LogError(object message) => ErrorCalls++;
}
```

`Program.cs`:

```csharp
using UnityEngine;

internal static class Program
{
    private static int _evaluations;

    private static object Probe(string message)
    {
        _evaluations++;
        return message;
    }

    private static int Main()
    {
        ZLog.Log(Probe("log"));
        ZLog.LogWarning(Probe("warning"));
        ZLog.LogError(Probe("error"));

#if UNITY_EDITOR
        bool passed = _evaluations == 3 && Debug.LogCalls == 1 &&
                      Debug.WarningCalls == 1 && Debug.ErrorCalls == 1;
#else
        bool passed = _evaluations == 0 && Debug.LogCalls == 0 &&
                      Debug.WarningCalls == 0 && Debug.ErrorCalls == 0;
#endif
        Console.WriteLine(passed ? "PASS" : "FAIL");
        return passed ? 0 : 1;
    }
}
```

- [x] **Step 2: Run the harness to verify RED**

Run: `dotnet run --project Temp/ZLogCompileTests/ZLogCompileTests.csproj -p:DefineConstants=UNITY_EDITOR`

Expected: compilation fails because the current `ZLog` has no static `Log`, `LogWarning`, or `LogError` methods.

- [x] **Step 3: Implement the minimal static facade**

```csharp
using UnityEngine;

public static class ZLog
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }
}
```

- [x] **Step 4: Verify GREEN in both compilation contexts**

Run:

```powershell
dotnet run --project Temp/ZLogCompileTests/ZLogCompileTests.csproj -p:DefineConstants=UNITY_EDITOR
dotnet run --project Temp/ZLogCompileTests/ZLogCompileTests.csproj
```

Expected: both exit with code 0; Editor observes three evaluations and three forwarded messages, Player observes zero evaluations and zero messages.

### Task 2: Route project logging through ZLog

**Files:**
- Modify: `Assets/Main/Main.cs`
- Modify: `Assets/ToolModule/其它/Tools.cs`
- Modify: `Assets/ToolModule/对象池/Pool.cs`
- Modify: `Assets/ToolModule/对象池/PoolManager.cs`

**Interfaces:**
- Consumes: the three `ZLog` methods from Task 1.
- Produces: no direct `Debug.Log*` calls under `Assets` outside `ZLog.cs`.

- [x] **Step 1: Add a failing residual-call check**

Search all C# files under `Assets`, excluding `ZLog.cs`, for `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError`.

- [x] **Step 2: Verify RED**

Run: `rg -n --glob '*.cs' --glob '!**/ZLog.cs' 'Debug\.Log(Error|Warning)?\s*\(' Assets`

Expected: existing call sites are reported.

- [x] **Step 3: Replace each direct call with its matching ZLog API**

Preserve messages and control flow exactly; change only the receiving class name.

- [x] **Step 4: Verify GREEN and compile the Unity project assembly**

Run the residual-call search and require no matches, rerun both temporary harness variants, then run `dotnet build Assembly-CSharp.csproj --no-restore` when the generated Unity project supports command-line compilation.

- [x] **Step 5: Remove the temporary harness**

Delete only the verified `Temp/ZLogCompileTests` directory after all checks pass.
