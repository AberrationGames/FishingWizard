using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FishingWizard.UI
{
    public class ButtonOnSelectedSubscriber : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public UnityEvent m_onSelectedEvent;
        public UnityEvent m_onDeselectedEvent;
        public UnityEvent m_onPressedEvent;
        
        public void OnSelect(BaseEventData a_eventData)
        {
            m_onSelectedEvent?.Invoke();   
        }
        public void OnDeselect(BaseEventData a_eventData)
        {
            m_onDeselectedEvent?.Invoke();   
        }

        public void OnPointerEnter(PointerEventData a_eventData)
        {
            m_onSelectedEvent?.Invoke();   
        }

        public void OnPointerExit(PointerEventData a_eventData)
        {
            m_onDeselectedEvent?.Invoke();   
        }
        
        public void OnPointerClick(PointerEventData a_eventData)
        {
            m_onPressedEvent?.Invoke();
        }
    }
}