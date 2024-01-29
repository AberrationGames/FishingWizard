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
        }

        void Update()
        {
            HandleMovementInput();
            HandleCameraInput();
        }

        private void HandleMovementInput()
        {
            Vector3 forceDirection = new Vector3(m_characterMoveSpeed * m_movementInput.x, 0, m_movementInput.y * m_characterMoveSpeed);
            m_rigidbody.velocity += forceDirection * Time.deltaTime;
        }

        void HandleCameraInput()
        {
            if (m_cameraTurnInput.x != 0)
            {
                Vector3 rotationAmount = new Vector3(0, m_cameraTurnInput.x * m_lookSpeedX * Time.deltaTime, 0);
                transform.Rotate(Vector3.up, rotationAmount.y);
            }

            if (m_cameraTurnInput.y != 0)
            {
                Vector3 eularAmount = m_playerCamera.transform.rotation.eulerAngles;
                eularAmount.x += -m_cameraTurnInput.y * m_lookSpeedY * Time.deltaTime;
                eularAmount.x = HelperFunctions.ClampAngle(eularAmount.x, m_minXCameraAngle, m_maxXCameraAngle);
                m_playerCamera.transform.localRotation = Quaternion.Euler(eularAmount);
            }
        }

        private void SetupInput()
        {
            m_input = new PlayerInput();
            m_input.Enable();
            m_input.Movement.Enable();
            m_input.Movement.WASD.started += WASDMovementStarted;
            m_input.Movement.WASD.performed += WASDMovementStarted;
            m_input.Movement.WASD.canceled += WASDMovementEnded;
            m_input.Movement.Camera.started += CameraMovementStarted;
            m_input.Movement.Camera.performed += CameraMovementStarted;
            m_input.Movement.Camera.canceled += CameraMovementEnded;
        }

        private void WASDMovementStarted(InputAction.CallbackContext a_context)
        {
            m_movementInput = a_context.ReadValue<Vector2>();
        }
        private void WASDMovementEnded(InputAction.CallbackContext a_context)
        {
            m_movementInput = Vector2.zero;
        }
        private void CameraMovementStarted(InputAction.CallbackContext a_context)
        {
            Debug.Log("Input Happened" + a_context.ReadValue<Vector2>());
            m_cameraTurnInput = a_context.ReadValue<Vector2>();
        }
        private void CameraMovementEnded(InputAction.CallbackContext a_context)
        {
            Debug.Log("Input Happened" + a_context.ReadValue<Vector2>());
            m_cameraTurnInput = Vector2.zero;
        }
    }
}