using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyMotor))]
[RequireComponent(typeof(EnemySenses))]
public abstract class EnemyBrain : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugDrawState = true;
    private IState initialState;
    private Camera debugCamera;

    public Health Health { get; private set; }
    public EnemyMotor Motor { get; private set; }
    public EnemySenses Senses { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public IState CurrentState => StateMachine.CurrentState;

    protected StateMachine StateMachine { get; private set; }

    protected virtual void Awake()
    {
        Health = GetComponent<Health>();
        Motor = GetComponent<EnemyMotor>();
        Senses = GetComponent<EnemySenses>();
        HomePosition = transform.position;
        debugCamera = Camera.main;

        StateMachine = new StateMachine();
        initialState = BuildStateMachine(StateMachine);
    }

    protected virtual void Start()
    {
        StateMachine.SetState(initialState);
    }

    protected virtual void OnEnable()
    {
        Health.Damaged += OnDamaged;
        Health.Died += HandleDied;
    }

    protected virtual void OnDisable()
    {
        Health.Damaged -= OnDamaged;
        Health.Died -= HandleDied;
    }

    protected virtual void Update()
    {
        if (Health.IsDead) return;

        StateMachine.Tick(Time.deltaTime);
    }

    protected abstract IState BuildStateMachine(StateMachine machine);

    protected virtual void OnDamaged(DamageInfo damage) { }

    private void HandleDied()
    {
        StateMachine.SetState(null);
        Motor.Stop();
        enabled = false;
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDrawState || debugCamera == null || CurrentState == null) return;

        Vector3 screenPoint = debugCamera.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPoint.z <= 0f) return;

        string stateName = CurrentState is EnemyState state ? state.Name : CurrentState.GetType().Name;
        string label = $"{stateName}  HP {Health.CurrentHealth:F0}/{Health.MaxHealth:F0}";
        GUI.Label(new Rect(screenPoint.x - 40f, Screen.height - screenPoint.y + 16f, 160f, 20f), label);
    }
}
