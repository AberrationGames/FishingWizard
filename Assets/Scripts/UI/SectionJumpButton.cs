using System;
using UnityEngine;
using UnityEngine.UI;

public class SectionJumpButton : MonoBehaviour
{
    public enum SectionToJumpTo
    {
        StartScreen = 0,
        NetworkSelectionScreenClient = 1,
        NetworkSelectionScreenHost,
        SettingsScreen,
    }
    [SerializeField] private SectionToJumpTo m_buttonSectionToJumpTo = SectionToJumpTo.StartScreen;
    
    private Button m_button;
    void Start()
    {
        m_button = GetComponent<Button>();
        switch (m_buttonSectionToJumpTo)
        {
            case SectionToJumpTo.StartScreen:
                m_button.onClick.AddListener(MainMenuManager.Instance.GotoStartScreen);
                break;
            case SectionToJumpTo.NetworkSelectionScreenClient:
                m_button.onClick.AddListener(MainMenuManager.Instance.GotoNetworkSelectionScreenClient);
                break;
            case SectionToJumpTo.NetworkSelectionScreenHost:
                m_button.onClick.AddListener(MainMenuManager.Instance.GotoNetworkSelectionScreenHost);
                break;
            case SectionToJumpTo.SettingsScreen:
                m_button.onClick.AddListener(MainMenuManager.Instance.GotoSettingsScreen);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
