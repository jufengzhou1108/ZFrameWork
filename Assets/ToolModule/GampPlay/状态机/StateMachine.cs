using System;
using System.Collections.Generic;

namespace ZFrameWork
{

public class StateMachine
{
    private readonly Dictionary<Type,StateBase> _stateDic=new Dictionary<Type, StateBase>();
    private StateBlackBoard _stateBlackBoard=new StateBlackBoard();
    private StateBase _curState;

    public StateBlackBoard BlackBoard => _stateBlackBoard;

    public void Init<T>() where T : StateBase,new()
    {
        if (_curState != null)
        {
            ZLog.LogError("状态机已初始化，重复 Init 被忽略。");
            return;
        }

        _curState=GetOrCreate<T>();
        _curState.SetStateMachine(this);
        _curState.Enter();
    }

    public void ChangeState<T>() where T : StateBase,new()
    {
        if (_curState == null)
        {
            ZLog.LogError("状态机尚未初始化，请先调用 Init 传入初始状态。");
            return;
        }

        if(_curState is T)
        {
            return;
        }

        _curState.Exit();

        _curState=GetOrCreate<T>();
        _curState.SetStateMachine(this);

        _curState.Enter();
    }

    public void Update()
    {
        if (_curState == null)
        {
            return;
        }

        _curState.Update();
    }

    private StateBase GetOrCreate<T>() where T : StateBase,new()
    {
        Type type=typeof(T);
        if (!_stateDic.TryGetValue(type, out StateBase state))
        {
            state=new T();
            _stateDic.Add(type,state);
        }
        return state;
    }
}
}
