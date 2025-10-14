using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPlayerBehavior : MonoBehaviour
{
    [Header("Refs")]
    public Transform SimulatedCameraTransform;
    public Transform SimulatedPlayerTransform;

    [SerializeField] private Camera DummyCamera;

    [Header("Settings")]
    [SerializeField] private float positionInterpolationAmount = .95f;
    [SerializeField] private float positionInterpolationSpeed = 4f;
    // This "half life" is how long it takes for the rotation error to halve
    [SerializeField] private float rotationInterpolationHalfLife = .1f;


    public void InitDummy(Transform SimCamTransform, Transform SimPlayerTranfsorm)
    {
        SimulatedCameraTransform = SimCamTransform;
        SimulatedPlayerTransform = SimPlayerTranfsorm;
    }

    private void LateUpdate()
    {
        // PLAYER //
        transform.position += (Vector3.Lerp(transform.position, SimulatedPlayerTransform.position, positionInterpolationAmount) - transform.position) * positionInterpolationSpeed * Time.deltaTime;

        // Found this solution to rotating independent of framrate
        float t = 1f - Mathf.Pow(0.5f, Time.deltaTime / rotationInterpolationHalfLife);
        transform.rotation = Quaternion.Slerp(transform.rotation, SimulatedPlayerTransform.rotation, t);


        // CAMERA //
        DummyCamera.transform.rotation = Quaternion.Slerp(DummyCamera.transform.rotation, SimulatedCameraTransform.rotation, t);
    }
}
