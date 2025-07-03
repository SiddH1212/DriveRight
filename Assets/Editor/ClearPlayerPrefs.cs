using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ClearPlayerPrefs
{
    [MenuItem("Tools/Clear PlayerPrefs")]
    public static void Clear()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared via menu.");
    }
}
