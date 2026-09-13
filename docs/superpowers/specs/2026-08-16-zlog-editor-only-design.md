# ZLog Editor-Only Logging Design

## Goal

Provide `ZLog.Log`, `ZLog.LogWarning`, and `ZLog.LogError` APIs that print normally in the Unity Editor while removing calls and argument evaluation from Player builds.

## Design

- Replace the empty `ZLog : MonoBehaviour` component with a stateless static class in the existing `ZLog.cs` file.
- Give each public method an `object message` parameter to match Unity's `Debug.Log` family and support strings, Unity objects, and value types.
- Apply `[System.Diagnostics.Conditional("UNITY_EDITOR")]` to every public logging method. When compiling without `UNITY_EDITOR`, the C# compiler omits each call expression, including evaluation of its arguments.
- Guard each internal `Debug.Log*` statement with `#if UNITY_EDITOR` so the wrapper implementation contains no Unity logging call in Player builds.
- Replace existing direct `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls under `Assets` with their corresponding `ZLog` calls.

## Behavior

| Build context | Result |
|---|---|
| Unity Editor | `ZLog` forwards messages to the matching `Debug.Log*` API. |
| Player build | Calls and argument evaluation are omitted; wrapper bodies contain no `Debug.Log*` calls. |

All methods return `void`, as required for conditional methods. Logging values through `object` may box value types in the Editor; no boxing occurs in Player code because conditional call sites are omitted.

## Verification

- Verify each API has the `Conditional("UNITY_EDITOR")` attribute.
- Verify the wrapper contains guarded `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls.
- Verify no direct `Debug.Log*` calls remain elsewhere under `Assets`.
- Compile with and without the `UNITY_EDITOR` symbol using a minimal test harness and inspect observable side effects to prove argument evaluation is retained only for Editor compilation.

## Scope

Do not add runtime log levels, file logging, formatting systems, configuration assets, or external dependencies.
