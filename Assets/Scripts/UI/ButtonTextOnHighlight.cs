using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace FishingWizard.UI
{
    [RequireComponent(typeof(ButtonOnSelectedSubscriber))]
    public class ButtonTextOnHighlight : MonoBehaviour
    {
        [SerializeField] private float m_selectedFontSize = 20;
        [SerializeField] private float m_regularFontSize = 16;
        [SerializeField] private float m_pressedFontSize = 18;
        
        [SerializeField] private Color m_selectedColor = Color.white;
        [SerializeField] private Color m_regularColor = Color.black;
        [SerializeField] private Color m_pressedColor = Color.gray;
        
        private ButtonOnSelectedSubscriber m_onSelectedSubscriber;
        private TextMeshProUGUI m_buttonText;
        
        private void Start()
        {
            m_onSelectedSubscriber = GetComponent<ButtonOnSelectedSubscriber>();
            m_buttonText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            
            m_onSelectedSubscriber.m_onSelectedEvent.AddListener(SetButtonHighlighted);
            m_onSelectedSubscriber.m_onDeselectedEvent.AddListener(SetButtonRegular);
            m_onSelectedSubscriber.m_onPressedEvent.AddListener(SetButtonPressed);
        }

        private void SetButtonHighlighted()
        {
            m_buttonText.fontSize = m_selectedFontSize;
            m_buttonText.color = m_selectedColor;
            MainMenuManager.Instance.m_selectedTexts.Add(this);
        }
        //Needs to be public so main menu manager can reset all ui buttons to their regular values between screen transitions eg start menu->settings->start menu
        public void SetButtonRegular()
        {
            m_buttonText.fontSize = m_regularFontSize;
            m_buttonText.color = m_regularColor;
        }

        private void SetButtonPressed()
        {
            m_buttonText.fontSize = m_pressedFontSize;
            m_buttonText.color = m_pressedColor;
        }
    }
}