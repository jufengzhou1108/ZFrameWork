# Replace Module Exceptions with ZLog Design

## Goal

Replace every explicit `throw` under `Assets` with `ZLog.LogError` and a safe immediate return, so framework modules do not propagate their own validation or loading exceptions.

## Error Policy

- Use the existing `ZLog.LogError(object message)` API; do not add or rename logging methods.
- Preserve each existing exception message as the logged message, excluding exception type metadata.
- After logging, return immediately. Never continue into code that assumed the failed condition was valid.
- In `void` methods, use `return`.
- In methods with a return value, use `return default`.
- In asynchronous Addressables completion handlers, log and return without invoking the success callback.
- Do not catch exceptions raised by external APIs; only replace the explicit `throw` statements currently owned by project modules.

## Module Changes

- `UnityTimer`: invalid durations and null callbacks log and return.
- `JsonTool`: missing directories or files log and return `default` from loads, or return from saves.
- `StateBlackBoard`: missing type/name entries log and return `default`.
- `AddressableManager`: failed asynchronous loads log and stop the completion path; failed synchronous loads log and return `default`.

## Verification

- Establish a failing baseline by finding all explicit `throw` statements under `Assets`.
- Add focused behavior tests where methods can be exercised without Unity runtime state, covering timer validation and missing blackboard data.
- Verify no explicit `throw` remains under `Assets` after implementation.
- Build `Assembly-CSharp.csproj` with zero compilation errors.

## Consequence

In Player builds, `ZLog.LogError` calls are removed by `Conditional("UNITY_EDITOR")`; failed operations therefore return silently with `default` or no result. Callers remain responsible for handling default values.
