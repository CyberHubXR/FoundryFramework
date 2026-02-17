using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.XR;
using Foundry;
using System.Threading;


namespace Foundry
{
    public class LocalPlayerMenuManager : MonoBehaviour
    {
        [Header("Menu Prefab")]
        [SerializeField] private GameObject playerMenuPrefab;
        
        [Header("Positioning")] 
        [SerializeField] private float forwardOffset = 1.5f;
        [SerializeField] private float verticalOffset = 0.1f;

        [SerializeField] private Camera uiCamera;

        private GameObject menuInstance;
        private Transform head;
        private Player localPlayer;

        [Header("Input")]
        [Tooltip("XR Primary Button / Desktop Key (M)")]
        public InputActionProperty toggleMenuAction;

        
        void Start()
        {
            if (!uiCamera)
            {
                // try to find a game object called "UICamera"
                var uiCameraObj = GameObject.Find("UICamera");
                if (uiCameraObj)
                {
                    uiCamera = uiCameraObj.GetComponent<Camera>();
                }
                else
                {
                    Debug.LogWarning("[LocalPlayerMenuManager] No UICamera found in scene");
                }
            }
        }

        void OnEnable()
        {
            if (toggleMenuAction.action != null)
            {
                toggleMenuAction.action.performed += OnTogglePerformed;
                toggleMenuAction.action.Enable();
            }
        }

        void OnDisable()
        {
            if (toggleMenuAction.action != null)
            {
                toggleMenuAction.action.performed -= OnTogglePerformed;
                toggleMenuAction.action.Disable();
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            ToggleMenu();
        }
        
        public void Initialize(Player player)
        {
            if (!playerMenuPrefab)
            {
                Debug.LogWarning("[LocalPlayerMenuManager] No playerMenuPrefab assigned");
                return;
            }
            localPlayer = player;

            head = player.trackers.head;
            
            if (!head)
            {
                Debug.LogWarning("[LocalPlayerMenuManager] Player has no head tracker");
                return;
            }

            // Instantiate locally (NOT networked)
            menuInstance = Instantiate(playerMenuPrefab);
            menuInstance.SetActive(false);

            // Inject Player into Menu
            var menu = menuInstance.GetComponent<LocalPlayerMenuActions>();
            if (menu != null)
            {
                menu.Initialize(localPlayer);
            }
            else
            {
                Debug.LogWarning("[LocalPlayerMenuManager] No LocalPlayerMenuActions found on playerMenuPrefab");
            }

            // Canvas setup
            var canvas = menuInstance.GetComponentInChildren<Canvas>(true); 
            if (canvas)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = uiCamera;
            }
            else
            {
                Debug.LogWarning("[LocalPlayerMenuManager] No Canvas found in playerMenuPrefab");
            }
        }

        public void ToggleMenu()
        {
            if (!menuInstance || !head)
                return;

            bool show = !menuInstance.activeSelf;

            if (show)
                RecenterMenu();

            menuInstance.SetActive(show);
        }

        private void RecenterMenu()
        {
            // Yaw-only rotation
            Vector3 forward = head.forward;
            forward.y = 0f;
            forward.Normalize();

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            Vector3 position =
                head.position +
                forward * forwardOffset +
                Vector3.up * verticalOffset;

            menuInstance.transform.SetPositionAndRotation(position, rotation);
        }

        

    }   
}
