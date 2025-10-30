using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class CameraPosition : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cam == null)
            Debug.LogWarning("Camera not assigned in CameraPosition script attached to " + gameObject.name);

        Camera[] cams = Camera.allCameras;
        Debug.Log("Cameras found: " + cams.Length);
        foreach (Camera c in cams)
        {
            if (c != cam)
            {
                DisableCameraAndAudio(c);
                Debug.Log("Disabled camera: " + c.name);
            }
            else
            {
                EnableCameraAndAudio(c);
                Debug.Log("Ensabled camera: " + c.name);
            }
        }
    }

    public void DisableCameraAndAudio(Camera c)
    {
        // Desabilita o componente Camera (para de renderizar)
        c.enabled = false;

        // Desabilita o componente AudioListener (para de ouvir áudio)
        AudioListener audioListener = c.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
    }

    public void EnableCameraAndAudio(Camera c)
    {
        // Habilita o componente Camera (começa a renderizar)
        c.enabled = true;

        // Habilita o componente AudioListener (começa a ouvir áudio)
        AudioListener audioListener = c.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
