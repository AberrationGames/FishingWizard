using UnityEngine;

public class SimpleRotateOverTime : MonoBehaviour
{
    [Header("0=X 1=Y 2=Z")]
    [SerializeField] private int m_axis = 1;
    [SerializeField] private float m_rotateSpeed = 5f;
    [SerializeField] private bool m_useLocalSpace = false;
    void Update()
    {
        Space rotateSpace = m_useLocalSpace ? Space.Self : Space.World;
        if (m_axis == 0)
            transform.Rotate(new Vector3(m_rotateSpeed * Time.deltaTime, 0, 0), rotateSpace);
        else if (m_axis == 1)
            transform.Rotate(new Vector3(0, m_rotateSpeed * Time.deltaTime, 0), rotateSpace);
        else if (m_axis == 2)
            transform.Rotate(new Vector3(0, 0, m_rotateSpeed * Time.deltaTime), rotateSpace);
    }
}
