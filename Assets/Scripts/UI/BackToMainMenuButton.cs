using UnityEngine;
using UnityEngine.UI;

public class BackToMainMenuButton : MonoBehaviour
{
    private Button m_button;
    void Start()
    {
        m_button = GetComponent<Button>();
        m_button.onClick.AddListener(MainMenuManager.Instance.GotoStartScreen);
    }
}
