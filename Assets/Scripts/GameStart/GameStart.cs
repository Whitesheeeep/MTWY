using System;
using UnityEngine;
using WS_Modules.UIModule;

public class GameStart : MonoBehaviour
{
    private void Start()
    {
        UIManager.Instance.PopUpWindow<GlobalUIWindow>();
    }
}
