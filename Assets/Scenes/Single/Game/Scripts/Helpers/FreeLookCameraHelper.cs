// using UnityEngine;
// using Unity.Cinemachine;

// public class FreeLookCameraHelper : MonoBehaviour
// {
//     public CinemachineCamera freeLookCam;
//     public float rotationSpeed = 300f;

//     void Update()
//     {
//         if (Input.GetMouseButton(0))
//         {
//             float mouseX = Input.GetAxis("Mouse X");
//             float mouseY = Input.GetAxis("Mouse Y");

//             freeLookCam.GetInputAxisProvider+= mouseX * rotationSpeed * Time.deltaTime;
//             freeLookCam.m_YAxis.Value -= mouseY * rotationSpeed * Time.deltaTime;
//         }
//     }
// }
