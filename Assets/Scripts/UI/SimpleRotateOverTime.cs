using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotateOverTime : MonoBehaviour
{
    [SerializeField] private float m_rotateSpeed = 5f;
    void Update()
    {
        transform.Rotate(new Vector3(0, m_rotateSpeed * Time.deltaTime, 0), Space.World);
    }
}
