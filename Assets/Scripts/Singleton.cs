using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set;  }

    protected virtual void Awake()
    {
        if (Instance == null)
            Instance = GetComponent<T>();
        else
        {
            Debug.Log($"Another instance of type '{nameof(T)}' already exists, destroying object");
            Destroy(gameObject);
        }
    }
}
