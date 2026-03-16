using UnityEngine;

[ExecuteInEditMode]
public class ShaderTracker : MonoBehaviour
{
    private Material _lastMaterial;
    private Shader _lastShader;
    private UnityEngine.UI.Graphic _graphic;

    void OnEnable()
    {
        _graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (_graphic != null)
        {
            _lastMaterial = _graphic.material;
            _lastShader = _lastMaterial != null ? _lastMaterial.shader : null;
        }
    }

    void Update()
    {
        if (_graphic == null) return;

        // Check if the Material instance changed
        if (_graphic.material != _lastMaterial)
        {
            Debug.Log($"<color=red>Material Changed!</color> New: {_graphic.material.name}", gameObject);
            Debug.Log(System.Environment.StackTrace); // Shows what script called this
            _lastMaterial = _graphic.material;
        }

        // Check if the Shader on the material changed
        if (_graphic.material != null && _graphic.material.shader != _lastShader)
        {
            Debug.Log($"<color=cyan>Shader Swapped!</color> New: {_graphic.material.shader.name}", gameObject);
            Debug.Log(System.Environment.StackTrace);
            _lastShader = _graphic.material.shader;
        }
    }
}