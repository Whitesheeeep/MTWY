using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using WS_Modules;
using WS_Modules.SceneModule;
using WS_Modules.UIModule;

public class GameStart : MonoBehaviour
{
    [WSScene, SerializeField] string firstAddtiveSceneName;

    private IEnumerator Start()
    {
        UIManager.Instance.PopUpWindow<GlobalUIWindow>();
        yield return SceneSystem.LoadSceneAsync(firstAddtiveSceneName, mode: LoadSceneMode.Additive).ToCoroutine();
        SceneSystem.SetActiveScene(firstAddtiveSceneName);
    }
}
