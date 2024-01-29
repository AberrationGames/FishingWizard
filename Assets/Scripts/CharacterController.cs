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
        [SerializeField] private GameObject m_lurePrefab;
        private GameObject m_currentLureObject;

        private Vector2 m_cameraTurnInput;
        private Vector2 m_movementInput;
        
        private PlayerInput m_input;

        //Keep individual camera so each player in the scene will be managing their own components/etc.
        private Camera m_playerCamera;
        private Rigidbody m_rigidbody;
        private FishingRod m_fishingRod;
        
        void Start()
        {
            m_playerCamera = Camera.main;
            m_rigidbody = GetComponent<Rigidbody>();
            m_fishingRod = GetComponentInChildren<FishingRod>();
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

            m_input.Movement.Interact.started += InteractInputStarted;
            
            m_input.Movement.Fish.started += CastLureStarted;
            
            m_input.Movement.Reel.started += ReelInputStarted;
            m_input.Movement.Reel.performed += ReelInputPerformed;
            m_input.Movement.Reel.canceled += ReelInputEnded;
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

        private void CastLureStarted(InputAction.CallbackContext a_context)
        {
            if (m_currentLureObject != null)
            {
                Destroy(m_currentLureObject);
            }
            GameObject lureObject = Instantiate(m_lurePrefab, m_fishingRod.m_rodTipObject.transform.position + m_fishingRod.m_rodTipObject.transform.forward * 3, Quaternion.identity);
            lureObject.transform.parent = m_fishingRod.m_rodTipObject.transform;
            lureObject.GetComponent<Rigidbody>().velocity = m_fishingRod.m_rodTipObject.forward * m_castLaunchVelocity;
            m_fishingRod.m_targetObject = lureObject.transform;
            m_currentLureObject = lureObject;
        }

        private void ReelInputStarted(InputAction.CallbackContext a_context)
        {
            
        }

        private void ReelInputPerformed(InputAction.CallbackContext a_context)
        {
            
        }

        private void ReelInputEnded(InputAction.CallbackContext a_context)
        {
            
        }

        private void InteractInputStarted(InputAction.CallbackContext a_context)
        {
            
        }
    }
}