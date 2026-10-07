
namespace ZFrameWork
{

public abstract class StateBase
{
    protected StateMachine _stateMachine;
    public void SetStateMachine(StateMachine stateMachine)
    {
        _stateMachine=stateMachine;
    }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Update();
}
}
