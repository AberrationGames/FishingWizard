using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FishingRod : MonoBehaviour
{
    //Funny joke to be made that a line renderer is rendering a fishing line heh kill me
    [SerializeField] private LineRenderer m_lineRenderer;
    public Transform m_rodTipObject;
    public Transform m_targetObject;
    void Start()
    {
        m_lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (m_rodTipObject == null || m_targetObject == null)
        {
            m_lineRenderer.enabled = false;   
            return;
        }

        m_lineRenderer.enabled = true;
        m_lineRenderer.SetPositions(new []{m_rodTipObject.position, m_targetObject.position});
    }
}
