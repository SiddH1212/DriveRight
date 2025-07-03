using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EXIT : MonoBehaviour
{
    public Button exit;
    void Start()
    {
        exit.onClick.AddListener(onClickExit);
    }
    public void onClickExit()
    {
        SceneManager.LoadScene("Start_Quest");
    }
}
