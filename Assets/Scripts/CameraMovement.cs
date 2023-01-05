using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private float cameraSpeed = 50.0f;

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            movement += transform.up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement += -transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += -transform.up;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += transform.right;
        }

        transform.position += movement.normalized * cameraSpeed * Time.unscaledDeltaTime;
    }
}
