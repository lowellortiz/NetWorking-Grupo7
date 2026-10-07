using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>Applies movement supplied by PlayerInputSource. It has no knowledge of keys.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputSource inputSource;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private Vector2 horizontalBounds = new(10.5f, 7.5f);

        public PlayerInputSource InputSource => inputSource;

        public void Configure(PlayerInputSource source, float speed, Vector2 bounds)
        {
            inputSource = source;
            moveSpeed = speed;
            horizontalBounds = bounds;
        }

        private void Update()
        {
            if (inputSource != null)
            {
                Move(inputSource.Movement, Time.deltaTime);
            }
        }

        public void Move(Vector2 input, float deltaTime)
        {
            Vector2 normalizedInput = Vector2.ClampMagnitude(input, 1f);
            Vector3 displacement = new Vector3(normalizedInput.x, 0f, normalizedInput.y) * (moveSpeed * deltaTime);
            Vector3 next = transform.position + displacement;
            next.x = Mathf.Clamp(next.x, -horizontalBounds.x, horizontalBounds.x);
            next.z = Mathf.Clamp(next.z, -horizontalBounds.y, horizontalBounds.y);
            transform.position = next;
        }
    }
}
