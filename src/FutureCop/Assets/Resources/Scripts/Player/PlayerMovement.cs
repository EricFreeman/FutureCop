using UnityEngine;

namespace Assets.Resources.Scripts.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        public float MaxSpeed = 3;
        public float Acceleration = 1f;
        public float Jump = 30;
        public LayerMask LayerMaskForGrounded;

        private float _movement;
        private float _jump;
        private Vector2 _knockback;

        private bool _isGrounded;

        private float _deadzone = .1f;

        private Rigidbody2D _rigidbody;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            _isGrounded = IsGrounded();

            CalculateMovement();
            CalculateJump();

            _rigidbody.velocity = new Vector2(_movement + _knockback.x, _rigidbody.velocity.y + _jump + _knockback.y);
        }

        private void CalculateJump()
        {
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                _jump = Jump;
            }
            else
            {
                _jump = 0;
            }
        }

        private void CalculateMovement()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontal) > _deadzone)
            {
                if (Mathf.Abs(_movement) < MaxSpeed)
                {
                    _movement += Mathf.Min(horizontal * Acceleration, MaxSpeed);
                }
            }
            else
            {
                _movement *= .7f;
            }
        }

        private bool IsGrounded()
        {
            var bounds = transform.GetComponent<SpriteRenderer>().bounds;
            var start = bounds.center - new Vector3(0, bounds.extents.y);
            var length = bounds.extents.y / 2;

            Debug.DrawRay(start, Vector3.down * length);
            var hit = Physics2D.Raycast(start, Vector3.down, length, 1 << LayerMaskForGrounded.value);
            
            return hit;
        }
    }
}