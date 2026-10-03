using UnityEngine;

namespace HollowSaint.Preview
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HollowSaintPreviewMotor : MonoBehaviour
    {
        public Animator animator;
        public Camera followCamera;
        public string[] clipNames;
        public string currentState = "Idle";
        public bool inspectClips;
        private CharacterController controller;
        private Vector3 cameraOffset;
        private float verticalSpeed, holdUntil, dashUntil, dashStarted;
        private Vector3 dashDirection;
        private bool wasGrounded = true, wasMoving, wasGliding;
        private bool scripted, scriptedJump, scriptedGlide, scriptedDash, scriptedCast;
        private Vector2 scriptedMove;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!followCamera) followCamera = Camera.main;
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!followCamera || !animator) throw new System.InvalidOperationException("Preview requires an Animator and camera");
            cameraOffset = followCamera.transform.position - transform.position;
            animator.applyRootMotion = false;
            // Keep preview poses and socket signals updating even outside a rendered Game view.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) inspectClips = !inspectClips;
            if (Input.GetKeyDown(KeyCode.R)) ResetPosition();
            if (inspectClips) return;
            Vector3 direction = ReadDirection();
            bool moving = direction.sqrMagnitude > 0.01f;
            bool grounded = controller.isGrounded;
            bool gliding = (scripted ? scriptedGlide : Input.GetKey(KeyCode.LeftShift)) && moving && grounded;
            if (grounded && verticalSpeed < 0) verticalSpeed = -2;
            if (grounded && (scripted ? scriptedJump : Input.GetKeyDown(KeyCode.Space)))
            {
                verticalSpeed = 6.5f;
                Play("Jump", 0.3f);
                grounded = false;
            }
            if ((scripted ? scriptedDash : Input.GetKeyDown(KeyCode.Q)) && Time.time >= dashUntil)
            {
                dashDirection = -transform.right;
                dashStarted = Time.time;
                dashUntil = Time.time + 0.65f;
                Play("Arc Step left start", 0.3f);
            }
            if ((scripted ? scriptedCast : Input.GetKeyDown(KeyCode.E)) && Time.time >= holdUntil) Play("Arc Bolt right", 0.8f);
            scriptedJump = scriptedDash = scriptedCast = false;
            Vector3 velocity = MoveVelocity(direction, moving, gliding, grounded);
            verticalSpeed -= 22 * Time.deltaTime;
            velocity.y = verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
            if (Time.time >= dashUntil && moving)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), 600 * Time.deltaTime);
            if (!wasGrounded && controller.isGrounded && verticalSpeed < 0) Play("Land", 0.55f);
            wasGrounded = controller.isGrounded;
            wasMoving = moving;
            wasGliding = gliding;
        }

        private Vector3 ReadDirection()
        {
            Vector3 forward = followCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector2 axes = scripted ? scriptedMove : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector3.ClampMagnitude(forward * axes.y + right * axes.x, 1);
        }

        public void DriveForValidation(Vector2 move, bool jump = false, bool glide = false, bool dash = false, bool cast = false)
        {
            scripted = true;
            scriptedMove = move;
            scriptedJump = jump;
            scriptedGlide = glide;
            scriptedDash = dash;
            scriptedCast = cast;
        }

        private Vector3 MoveVelocity(Vector3 direction, bool moving, bool gliding, bool grounded)
        {
            if (Time.time < dashUntil)
            {
                float elapsed = Time.time - dashStarted;
                if (elapsed >= 0.3f && elapsed < 0.45f) Play("Arc Step left loop");
                if (elapsed >= 0.45f) Play("Arc Step left end");
                return elapsed < 0.45f ? dashDirection * 12 : Vector3.zero;
            }
            if (Time.time >= holdUntil)
            {
                if (!grounded) Play(verticalSpeed > 0 ? "Ascend" : "Descend");
                else if (gliding && !wasGliding) Play("Glide enter", 0.5f);
                else if (gliding) Play("Glide loop");
                else if (wasGliding) Play("Glide exit", 0.55f);
                else if (moving && !wasMoving) Play("Run start", 0.65f);
                else if (!moving && wasMoving) Play("Run stop", 0.65f);
                else Play(moving ? "Run forward" : "Idle");
            }
            return direction * (gliding ? 8.7f : 6);
        }

        public void Play(string title, float hold = 0)
        {
            if (currentState != title)
            {
                animator.CrossFadeInFixedTime(title, 0.08f);
                currentState = title;
            }
            if (hold > 0) holdUntil = Time.time + hold;
        }

        private void LateUpdate()
        {
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
                transform.position + cameraOffset, 1 - Mathf.Exp(-8 * Time.deltaTime));
            followCamera.transform.LookAt(transform.position + Vector3.up * 1.25f);
        }

        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(new Vector3(0, 0.05f, 0), Quaternion.identity);
            controller.enabled = true;
            verticalSpeed = 0;
            dashUntil = holdUntil = 0;
            inspectClips = false;
            Play("Idle");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 290, Screen.height - 32), GUI.skin.box);
            GUILayout.Label("HOLLOW SAINT — IMPORT PROOF");
            GUILayout.Label("WASD move · Shift glide · Space jump");
            GUILayout.Label("Q left dash · E cast · R reset");
            GUILayout.Label("Tab: clip inspection");
            GUILayout.Label("State: " + currentState);
            GUILayout.Label("Local test scene; not RoR2 gameplay.");
            if (inspectClips)
                foreach (string title in clipNames)
                    if (GUILayout.Button(title))
                    {
                        animator.Play(title, 0, 0);
                        currentState = title;
                    }
            GUILayout.EndArea();
        }
    }
}
