using UnityEngine;
using UnityEngine.InputSystem;

namespace FishingWizard
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private float m_characterMoveSpeed = 20f;
        [SerializeField] private float m_lookSpeedX = 20f;
        [SerializeField] private float m_lookSpeedY = 20f;
        [SerializeField] private float m_minXCameraAngle = 20f;
        [SerializeField] private float m_maxXCameraAngle = 20f;
        [SerializeField] private float m_castLaunchVelocity = 20f;

        private Vector2 m_cameraTurnInput;
        private Vector2 m_movementInput;
        
        private PlayerInput m_input;

        //Keep individual camera so each player in the scene will be managing their own components/etc.
        private Camera m_playerCamera;
        private Rigidbody m_rigidbody;

        void Start()
        {
            m_playerCamera = Camera.main;
            m_rigidbody = GetComponent<Rigidbody>();
            SetupInput();
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            HandleMovementInput();
            HandleCameraInput();
        }

        private void HandleMovementInput()
        {

            Vector3 forwardForce = transform.forward;
            Vector3 horizontalForce = transform.right;
            horizontalForce *= m_movementInput.x * m_characterMoveSpeed;
            forwardForce *= m_movementInput.y * m_characterMoveSpeed;
            m_rigidbody.velocity += (horizontalForce + forwardForce) * Time.deltaTime;
        }

        void HandleCameraInput()
        {
            float rotationAmount = m_cameraTurnInput.x * m_lookSpeedX * Time.deltaTime;
            float xValue = -m_cameraTurnInput.y * m_lookSpeedY * Time.deltaTime;
            
            xValue = HelperFunctions.ClampAngle(xValue, m_minXCameraAngle, m_maxXCameraAngle);
            m_playerCamera.transform.rotation *= Quaternion.Euler(xValue, 0, -m_playerCamera.transform.rotation.eulerAngles.z);
            transform.rotation *= Quaternion.Euler(0, rotationAmount,0);
        }

        private void SetupInput()
        {
            m_input = new PlayerInput();
            m_input.Enable();
            m_input.Movement.Enable();
            m_input.Movement.WASD.started += WASDMovementStarted;
            m_input.Movement.WASD.performed += WASDMovementStarted;
            m_input.Movement.WASD.canceled += WASDMovementEnded;
            
            m_input.Movement.CameraX.started += CameraMovementXStarted;
            m_input.Movement.CameraX.performed += CameraMovementXStarted;
            m_input.Movement.CameraX.canceled += CameraMovementXEnded;
            
            m_input.Movement.CameraY.started += CameraMovementYStarted;
            m_input.Movement.CameraY.performed += CameraMovementYStarted;
            m_input.Movement.CameraY.canceled += CameraMovementYEnded;
        }

        private void WASDMovementStarted(InputAction.CallbackContext a_context)
        {
            m_movementInput = a_context.ReadValue<Vector2>();
        }
        private void WASDMovementEnded(InputAction.CallbackContext a_context)
        {
            m_movementInput = Vector2.zero;
        }
        private void CameraMovementXStarted(InputAction.CallbackContext a_context)
        {
            m_cameraTurnInput.x = a_context.ReadValue<float>();
        }
        private void CameraMovementXEnded(InputAction.CallbackContext a_context)
        {
            m_cameraTurnInput.x = 0;
        }
        private void CameraMovementYStarted(InputAction.CallbackContext a_context)
        {
            m_cameraTurnInput.y = a_context.ReadValue<float>();
        }
        private void CameraMovementYEnded(InputAction.CallbackContext a_context)
        {
            m_cameraTurnInput.y = 0;
        }
    }
}