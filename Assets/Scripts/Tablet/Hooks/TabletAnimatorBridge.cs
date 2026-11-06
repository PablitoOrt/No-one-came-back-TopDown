using UnityEngine;

namespace NoOneCameBack.Tablet
{
    public class TabletAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private TabletController tablet;
        [SerializeField] private Animator animator;
        [SerializeField] private string takeOutTrigger = "TabletTakeOut";
        [SerializeField] private string putAwayTrigger = "TabletPutAway";
        [Tooltip("Skip the animation while the tablet is broken (System menu before finding it).")]
        [SerializeField] private bool onlyWhenFound = true;

        private void OnEnable()
        {
            if (tablet == null) return;

            tablet.TakeOutStarted += OnTakeOut;
            tablet.PutAwayStarted += OnPutAway;
        }

        private void OnDisable()
        {
            if (tablet == null) return;

            tablet.TakeOutStarted -= OnTakeOut;
            tablet.PutAwayStarted -= OnPutAway;
        }

        private void OnTakeOut() => Fire(takeOutTrigger);

        private void OnPutAway() => Fire(putAwayTrigger);

        private void Fire(string trigger)
        {
            if (animator == null || string.IsNullOrEmpty(trigger)) return;
            if (onlyWhenFound && !tablet.IsFound) return;
            if (!HasTrigger(trigger)) return;

            animator.SetTrigger(trigger);
        }

        // Lets the bridge sit on the player before the animator controller has the tablet triggers, without warnings.
        private bool HasTrigger(string trigger)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == trigger) return true;
            }
            return false;
        }
    }
}
