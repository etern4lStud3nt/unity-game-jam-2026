using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Loads another scene behind a black screen: waits, fades out, loads (replacing the current scenes), fades back in.
// Runs on its own hidden object that survives the load, so it can be started by something that is about to vanish
// (a pickup that pops). Player input is frozen until the new scene is in.
public static class SceneTransition
{
    private class Runner : MonoBehaviour { }

    private static bool running;

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => running = false;

    // Scene must be in File > Build Profiles > Scene List. fadeIn 0 = the new scene starts black and has to fade
    // itself in.
    public static void Load(string scene, float delay = 0f, float fadeOut = 0.5f, float fadeIn = 0.5f)
    {
        if (running)
            return;
        running = true;
        var host = new GameObject("Scene Transition");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<Runner>().StartCoroutine(Run(host, scene, delay, fadeOut, fadeIn));
    }

    private static IEnumerator Run(GameObject host, string scene, float delay, float fadeOut, float fadeIn)
    {
        InputManager input = InputManager.Instance;
        if (input != null)
            input.BlockGameplay();

        yield return new WaitForSeconds(delay);
        yield return ScreenFade.To(1f, fadeOut);

        AsyncOperation loading = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        if (loading == null)
        {
            Debug.LogError($"SceneTransition: scene \"{scene}\" could not be loaded. Is it in the Build Profiles scene list?");
            if (input != null)
                input.UnblockGameplay();
            yield return ScreenFade.To(0f, fadeIn);
        }
        else
        {
            while (!loading.isDone)
                yield return null;
            if (fadeIn > 0f)
                yield return ScreenFade.To(0f, fadeIn);
        }

        running = false;
        Object.Destroy(host);
    }
}
