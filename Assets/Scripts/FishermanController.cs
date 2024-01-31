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
        private float m_slowDownPercentCalculated = 0;
        
        [SerializeField] private GameObject m_lurePrefab;
        private GameObject m_currentLureObject;

        private float m_inputEnterDelay = 0.7f;
        private float m_inputDelayTimer = 0.0f;
        
        //Container for each players lure. will add naming eg player1lure and etc so tracking is much easier.
        private GameObject m_lureContainer;

        private Vector2 m_cameraMovementInput;
        private Vector2 m_movementInput;
        
        private PlayerInput m_input;

        //Keep individual camera so each player in the scene will be managing their own components/etc.
        private Camera m_playerCamera;
        private Rigidbody m_rigidbody;
        private FishingRod m_fishingRod;

        private bool m_isNetworkSpawned = false;

        private struct InputSyncStruct : INetworkSerializable
        {
            public Vector2 m_movementInput;
            public Vector2 m_mouseInput;
            public bool m_leftMouseDown;
            public bool m_rightMouseDown;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref m_movementInput);
                serializer.SerializeValue(ref m_mouseInput);
                serializer.SerializeValue(ref m_leftMouseDown);
                serializer.SerializeValue(ref m_rightMouseDown);
            }
        }

        private NetworkVariable<InputSyncStruct> m_inputSyncVariable = new NetworkVariable<InputSyncStruct>();

        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                m_isNetworkSpawned = true;
            }
        }
        
        void Start()
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

        void Update()
        {
            m_inputDelayTimer += Time.deltaTime;
            if (m_inputDelayTimer < m_inputEnterDelay) 
                return;
            
            if (IsHost)
            {
                if (m_playerCamera != null && m_isNetworkSpawned)
                    SendTransformDataClientRpc(transform.position, m_playerCamera.transform.rotation.eulerAngles);
                HandleMovementInput();
                HandleCameraInput();
            }
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

            Vector3 eularRotation = m_playerCamera.transform.rotation.eulerAngles;
            eularRotation.x += xValue;
            eularRotation.x = HelperFunctions.ClampAngle(eularRotation.x, m_minXCameraAngle, m_maxXCameraAngle);
            m_playerCamera.transform.rotation = Quaternion.Euler(eularRotation);
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

        [ServerRpc]
        private void SyncInputServerRpc(Vector2 a_movementInput, Vector2 a_cameraMovementInput)
        {
            m_movementInput = a_movementInput;
            m_cameraMovementInput = a_cameraMovementInput;
        }

        [ClientRpc]
        private void SendTransformDataClientRpc(Vector3 a_position, Vector3 a_eularRotation)
        {
            transform.position = a_position;
            if (m_playerCamera != null)
                m_playerCamera.transform.rotation = Quaternion.Euler(a_eularRotation);
        }
        
        private void WASDMovementStarted(InputAction.CallbackContext a_context)
        {
            m_movementInput = a_context.ReadValue<Vector2>();
            if (!IsHost || !IsServer)
                SyncInputServerRpc(m_movementInput, m_cameraMovementInput);
        }
        private void WASDMovementEnded(InputAction.CallbackContext a_context)
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