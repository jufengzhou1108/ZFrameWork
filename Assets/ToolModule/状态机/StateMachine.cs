using System;
using System.Collections.Generic;
using System.Linq;

namespace ZFrameWork
{

public class StateMachine
{
    //状态容器限制10个状态，超出上限就移除状态
    private readonly Dictionary<Type,StateBase> _stateDic=new Dictionary<Type, StateBase>(10);
    private StateBlackBoard _stateBlackBoard=new StateBlackBoard();
    private StateBase _curState;

    /// <summary>
    /// 状态机黑板，用于状态间共享数据
    /// </summary>
    public StateBlackBoard BlackBoard => _stateBlackBoard;

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void ChangeState<T>() where T : StateBase,new()
    {
        //不能切换到同一状态
        if(_curState is T)
        {
            return;
        }

        _curState?.Exit();

        Type type=typeof(T);
        if (!_stateDic.ContainsKey(type))
        {
            //移除元素，为新状态腾位置
            while (_stateDic.Count >= 10)
            {
                Type delType=_stateDic.Keys.ElementAt(0);
                _stateDic.Remove(delType);
            }
            _stateDic.Add(type,new T());
        }
        _curState=_stateDic[type];
        _curState.SetStateMachine(this);

        _curState.Enter();
    }

    /// <summary>
    /// 调用更新方法
    /// </summary>
    public void Update()
    {
        _curState.Update();
    }
}
}
