using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_top_down : MonoBehaviour
{
    public Transform m_target;

    [SerializeField] private float m_height = 10f;
    [SerializeField] private float m_distance = 20f;
    [SerializeField] private float m_angle = 45f;
    [SerializeField] private float m_smoothSpeed = 0.5f;

    private Vector3 refVelocity;

    void Start() 
    {
        handleCamera();
    }

    void Update() 
    {
        handleCamera();
    }

    protected virtual void handleCamera() 
    {
        if (!m_target){
            return;
        }

        //build world position Vector
        Vector3 worldPosition = (Vector3.forward * -m_distance) + (Vector3.up * m_height);
        Debug.DrawLine(m_target.position, worldPosition, Color.red);

        // build rotated vector
        Vector3 rotatedVector = Quaternion.AngleAxis(m_angle, Vector3.up) * worldPosition;
        Debug.DrawLine(m_target.position, worldPosition, Color.green);

        //move position
        Vector3 flatTargetPosition = m_target.position;
        flatTargetPosition.y = 0f;
        Vector3 finalPosition = flatTargetPosition + rotatedVector;
        Debug.DrawLine(m_target.position, finalPosition, Color.blue);

        //transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref refVelocity, m_smoothSpeed);
        //transform.LookAt(flatTargetPosition);
    }




}
