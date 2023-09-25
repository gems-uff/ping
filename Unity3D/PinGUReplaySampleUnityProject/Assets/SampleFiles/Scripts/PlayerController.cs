using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _jumpForce;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private PlayerProvenance _playerProvenance;
    
    private void Update()
    {
        var horizontalAxis = Input.GetAxisRaw("Horizontal");
        _rigidbody2D.velocity = new Vector2(horizontalAxis * _moveSpeed, _rigidbody2D.velocity.y);
        
        if(Input.GetButtonDown("Jump") && Mathf.Abs(_rigidbody2D.velocity.y) == 0f)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, 0);
            _rigidbody2D.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.CompareTag("Collectable"))
            return;
        
        _playerProvenance.OnCollectCollectable(other.gameObject);
        Destroy(other.gameObject);
        GameController.OnGetCollectable();    
    }
}
