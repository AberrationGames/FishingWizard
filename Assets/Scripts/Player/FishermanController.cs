using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FishingWizard
{
    public class FishermanController : NetworkBehaviour
    {
        [SerializeField] private float m_characterMoveSpeed = 20f;
        [SerializeField] private float m_lookSpeedX = 20f;
        [SerializeField] private float m_lookSpeedY = 20f;
        [SerializeField] private float m_minXCameraAngle = 20f;
        [SerializeField] private float m_maxXCameraAngle = 20f;
        [SerializeField] private float m_castLaunchVelocity = 20f;
        [Header("% of velocity lost per update when no input is found")]
        [SerializeField] private float m_slowDownPercent = 2;
        //slow down calculated but just makes it easier to see the % as a raw int value.
        private float m_slowDownPercentCalculated;
        
        [SerializeField] private GameObject m_lurePrefab;
        private GameObject m_currentLureObject;

        private const float InputEnterDelay = 0.7f;
        private float m_inputDelayTimer;
        
        //Container for each players lure. will add naming eg player1lure and etc so tracking is much easier.
        private GameObject m_lureContainer;

        private Vector2 m_cameraMovementInput;
        private Vector2 m_movementInput;
        
        private PlayerInput m_input;

        //Keep individual camera so each player in the scene will be managing their own components/etc.
        private Camera m_playerCamera;
        private Rigidbody m_rigidbody;
        private FishingRod m_fishingRod;

        private bool m_isNetworkSpawned;

        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                m_isNetworkSpawned = true;
            }
        }
        
        private void Start()
        {
            m_playerCamera = GetComponentInChildren<Camera>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_fishingRod = GetComponentInChildren<FishingRod>();
            m_lureContainer = GameObject.Find("LureContainer");
            m_slowDownPercentCalculated = (1 - (m_slowDownPercent / 100));

            if (!IsOwner)
            {
                //8 = do render 
                gameObject.layer = 8;
                //Disable the cameras on the other characters so no overriding occurs.
                GetComponentInChildren<AudioListener>().enabled = false;
                GetComponentInChildren<Camera>().enabled = false;
                for (int i = 0; i < transform.GetChild(0).childCount; i++)
                {
                    Transform child = transform.GetChild(0).GetChild(0);
                    if (child.gameObject.name == "LookDirection")
                        child.gameObject.layer = 8;
                }
                return;
            }
            
            //Otherwise we dont want to see the model 
            gameObject.layer = 7;
            SetupInput();
        }

        private void Update()
        {
            m_inputDelayTimer += Time.deltaTime;
            if (!IsHost || m_inputDelayTimer < InputEnterDelay) 
                return;

            if (m_playerCamera != null && m_isNetworkSpawned)
                SendTransformDataClientRpc(transform.position, m_playerCamera.transform.rotation.eulerAngles);
            
            HandleMovementInput();
            HandleCameraInput();
        }

        private void HandleMovementInput()
        {
            if (m_movementInput != Vector2.zero)
            {
                Vector3 forwardForce = transform.forward;
                Vector3 horizontalForce = transform.right;
                horizontalForce *= m_movementInput.x * m_characterMoveSpeed;
                forwardForce *= m_movementInput.y * m_characterMoveSpeed;
                m_rigidbody.velocity += (horizontalForce + forwardForce) * Time.deltaTime;
            }
            else
            {
                Vector3 velocity = m_rigidbody.velocity;
                velocity.y = 0;
                velocity *= m_slowDownPercentCalculated;
                velocity.y = m_rigidbody.velocity.y;
                m_rigidbody.velocity = velocity;
            }
        }

        void HandleCameraInput()
        {
            float rotationAmount = m_cameraMovementInput.x * m_lookSpeedX;
            float xValue = -m_cameraMovementInput.y * m_lookSpeedY;

            Vector3 rotation = m_playerCamera.transform.rotation.eulerAngles;
            rotation.x += xValue;
            rotation.x = HelperFunctions.ClampAngle(rotation.x, m_minXCameraAngle, m_maxXCameraAngle);
            m_playerCamera.transform.rotation = Quaternion.Euler(rotation);
            transform.rotation *= Quaternion.Euler(0, rotationAmount,0);
        }
        
        private void CastLureFromRod()
        {
            if (m_currentLureObject != null)
            {
                Destroy(m_currentLureObject);
            }

            GameObject lureObject = Instantiate(m_lurePrefab, m_fishingRod.m_rodTipObject.transform.position + m_fishingRod.m_rodTipObject.transform.forward * 3, Quaternion.identity);
            lureObject.transform.parent = m_lureContainer.transform.transform;
            lureObject.GetComponent<Rigidbody>().velocity = m_playerCamera.transform.forward * m_castLaunchVelocity;
            m_fishingRod.m_targetObject = lureObject.transform;
            m_currentLureObject = lureObject;
        }
        private void StartReelingLure()
        {
            //throw new System.NotImplementedException();
        }
        private void ReelLure()
        {
        }
        private void StopReelingLure()
        {
        }

        #region RPCFunctions 

        [ServerRpc]
        private void SyncInputServerRpc(Vector2 a_movementInput, Vector2 a_cameraMovementInput)
        {
            m_movementInput = a_movementInput;
            m_cameraMovementInput = a_cameraMovementInput;
        }
        [ServerRpc]
        private void SyncMouseInputServerRpc(bool a_leftClickPressed, bool a_rightClickPressed)
        {
            if (a_leftClickPressed)
                CastLureFromRod();
            else if (a_rightClickPressed)
            {
                StartReelingLure();
            }
        }
        [ClientRpc]
        private void SendTransformDataClientRpc(Vector3 a_position, Vector3 a_rotation)
        {
            transform.position = a_position;
            if (m_playerCamera != null)
                m_playerCamera.transform.rotation = Quaternion.Euler(a_rotation);
        }

        #endregion

        #region InputFunctions 

        private void SetupInput()
        {
            m_input = new PlayerInput();
            m_input.Enable();
            m_input.Movement.Enable();
            m_input.Movement.WASD.started += WasdMovementStarted;
            m_input.Movement.WASD.performed += WasdMovementStarted;
            m_input.Movement.WASD.canceled += WasdMovementEnded;
            
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
        private void WasdMovementStarted(InputAction.CallbackContext a_context)
        {
            m_movementInput = a_context.ReadValue<Vector2>();
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void WasdMovementEnded(InputAction.CallbackContext a_context)
        {
            m_movementInput = Vector2.zero;
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void CameraMovementXStarted(InputAction.CallbackContext a_context)
        {
            m_cameraMovementInput.x = a_context.ReadValue<float>();
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void CameraMovementXEnded(InputAction.CallbackContext a_context)
        {
            m_cameraMovementInput.x = 0;
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void CameraMovementYStarted(InputAction.CallbackContext a_context)
        {
            m_cameraMovementInput.y = a_context.ReadValue<float>();
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void CameraMovementYEnded(InputAction.CallbackContext a_context)
        {
            m_cameraMovementInput.y = 0;
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }

        private void CastLureStarted(InputAction.CallbackContext a_context)
        {
            if (!IsHost)
            {
                SyncMouseInputServerRpc(true, false);
                return;   
            }
            CastLureFromRod();
        }

        private void ReelInputStarted(InputAction.CallbackContext a_context)
        {
            if (!IsHost)
            {
                SyncMouseInputServerRpc(false, true);
                return;   
            }
            StartReelingLure();
        }
        private void ReelInputPerformed(InputAction.CallbackContext a_context)
        {
            ReelLure();
        }
        private void ReelInputEnded(InputAction.CallbackContext a_context)
        {
            StopReelingLure();
        }
        private void InteractInputStarted(InputAction.CallbackContext a_context)
        {
            
        }

        #endregion
    }
}