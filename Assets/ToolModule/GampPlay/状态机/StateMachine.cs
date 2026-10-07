using System;
using System.Collections.Generic;

namespace ZFrameWork
{

public class StateMachine
{
    //状态容器，不设上限
    private readonly Dictionary<Type,StateBase> _stateDic=new Dictionary<Type, StateBase>();
    private StateBlackBoard _stateBlackBoard=new StateBlackBoard();
    private StateBase _curState;

    /// <summary>
    /// 状态机黑板，用于状态间共享数据
    /// </summary>
    public StateBlackBoard BlackBoard => _stateBlackBoard;

    /// <summary>
    /// 初始化状态机：由外部传入初始状态并进入。实例状态机，每个实例各自初始化一次。
    /// </summary>
    /// <typeparam name="T">初始状态类型</typeparam>
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

    /// <summary>
    /// 切换状态。必须先 Init 传入初始状态。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void ChangeState<T>() where T : StateBase,new()
    {
        if (_curState == null)
        {
            ZLog.LogError("状态机尚未初始化，请先调用 Init 传入初始状态。");
            return;
        }

        //不能切换到同一状态
        if(_curState is T)
        {
            return;
        }

        _curState.Exit();

        _curState=GetOrCreate<T>();
        _curState.SetStateMachine(this);

        _curState.Enter();
    }

    /// <summary>
    /// 调用更新方法。还没进入任何状态属正常瞬态，直接忽略。
    /// </summary>
    public void Update()
    {
        if (_curState == null)
        {
            return;
        }

        _curState.Update();
    }

    //按类型取状态，没有就创建一个（状态在状态机内长期复用）
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
