using UnityEngine;

public class BaseComponent : MonoBehaviour
{
    protected virtual void Awake()
    {
        GameManager.RegisterComponent(this);
    }
}
