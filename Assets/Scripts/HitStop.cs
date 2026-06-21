using System.Collections;
using UnityEngine;

// Хитстоп: на доли секунды замораживает время при попадании.
// Самый дешёвый способ придать удару "вес". Создаётся сам, вешать никуда не нужно.
public class HitStop : MonoBehaviour
{
    private static HitStop instance;
    private Coroutine routine;

    public static HitStop Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("HitStop");
                instance = go.AddComponent<HitStop>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Заморозить время на duration РЕАЛЬНЫХ секунд (например 0.05).
    public void Freeze(float duration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(DoFreeze(duration));
    }

    private IEnumerator DoFreeze(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration); // реальное время, т.к. игровое стоит
        Time.timeScale = 1f;
        routine = null;
    }
}
