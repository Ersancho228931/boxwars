using UnityEngine;

public class FixedHandScale : MonoBehaviour
{
    [Header("Fixed Scale for Objects in Hands")]
    [Tooltip("Normal size for carried objects")]
    public Vector3 fixedScale = new Vector3(0.1f, 0.1f, 1f);

    private void LateUpdate()
    {
        // This runs after animation scaling, forcing the object to stay at your desired size
        if (transform.parent != null)
        {
            transform.localScale = fixedScale;
        }
    }

    // Optional: Call this when object is picked up
    public void LockToFixedScale()
    {
        transform.localScale = fixedScale;
    }

    // Optional: Call this when object is dropped
    public void ResetToOriginalScale()
    {
        transform.localScale = Vector3.one; // or your prefab's original scale
    }
}