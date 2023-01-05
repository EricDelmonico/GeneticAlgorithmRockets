using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraSizing : MonoBehaviour
{
    [SerializeField]
    private Text screenSizeText;

    private Camera cam;

    private Slider camSizeSlider;
    [SerializeField]
    private float zoomSpeed = 50.0f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        camSizeSlider = GameObject.Find("ScreenSizeSlider").GetComponent<Slider>();
        camSizeSlider.value = cam.orthographicSize;
        ChangeScreenSize(cam.orthographicSize);
    }

    private void Update()
    {
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.0f)
        {
            cam.orthographicSize -= Input.mouseScrollDelta.y * zoomSpeed * Time.unscaledDeltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 1, 10);
            camSizeSlider.value = cam.orthographicSize;
        }
    }

    public void ChangeScreenSize(float size)
    {
        cam.orthographicSize = size;
        screenSizeText.text = "Screen Size: " + size;
    }
}
