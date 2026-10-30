using UnityEngine;

public class WeaponModel : MonoBehaviour
{
    [Tooltip("Tip of the barrel. Shots, tracers and the muzzle flash start here. Its blue axis (forward) should point out of the barrel.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Where shells are ejected. Left empty, the effects profile offset from the muzzle is used.")]
    [SerializeField] private Transform shellEject;

    public Transform Muzzle => muzzle != null ? muzzle : transform;
    public Transform ShellEject => shellEject;

    private void OnDrawGizmos()
    {
        Transform tip = Muzzle;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tip.position, 0.02f);
        Gizmos.DrawRay(tip.position, tip.forward * 0.3f);

        if (shellEject == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(shellEject.position, 0.015f);
        Gizmos.DrawRay(shellEject.position, shellEject.right * 0.15f);
    }
}
