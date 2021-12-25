using UnityEngine;

public class Outlined : MonoBehaviour
{
    public Renderer Renderer => mainRenderer;
    private Renderer mainRenderer;

    private void OnEnable()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponent<Renderer>();
    }
}
