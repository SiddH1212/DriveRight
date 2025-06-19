using UnityEngine;
using System;

[ExecuteAlways]
public class UniqueID : MonoBehaviour
{
    [SerializeField] private string uniqueID;

    public string ID => uniqueID;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
