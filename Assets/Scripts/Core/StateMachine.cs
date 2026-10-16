using System;
using System.Collections.Generic;

public interface IState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}

public class StateMachine
{
    private class Transition
    {
        public readonly IState To;
        public readonly Func<bool> Condition;

        public Transition(IState to, Func<bool> condition)
        {
            To = to;
            Condition = condition;
        }
    }

    private static readonly List<Transition> NoTransitions = new List<Transition>(0);

    private readonly Dictionary<IState, List<Transition>> transitions = new Dictionary<IState, List<Transition>>();
    private readonly List<Transition> anyTransitions = new List<Transition>();
    private List<Transition> currentTransitions = NoTransitions;

    public IState CurrentState { get; private set; }

    public event Action<IState, IState> StateChanged;

    public void AddTransition(IState from, IState to, Func<bool> condition)
    {
        if (!transitions.TryGetValue(from, out List<Transition> list))
        {
            list = new List<Transition>();
            transitions[from] = list;
        }

        list.Add(new Transition(to, condition));
    }

    public void AddAnyTransition(IState to, Func<bool> condition)
    {
        anyTransitions.Add(new Transition(to, condition));
    }

    public void Tick(float deltaTime)
    {
        IState next = FindTransition();
        if (next != null) SetState(next);

        CurrentState?.Tick(deltaTime);
    }

    public void SetState(IState state)
    {
        if (state == CurrentState) return;

        IState previous = CurrentState;
        previous?.Exit();

        CurrentState = state;
        currentTransitions = state != null && transitions.TryGetValue(state, out List<Transition> list) ? list : NoTransitions;

        state?.Enter();
        StateChanged?.Invoke(previous, state);
    }

    private IState FindTransition()
    {
        foreach (Transition transition in anyTransitions)
        {
            if (transition.To != CurrentState && transition.Condition()) return transition.To;
        }

        foreach (Transition transition in currentTransitions)
        {
            if (transition.Condition()) return transition.To;
        }

        return null;
    }
}
