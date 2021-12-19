using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class PlayerController : NetworkBehaviour
{
    private Rigidbody _rigidbody;
    private Animator _animator;
    private Vector2 _moveInput;
    private NetworkVariable<Unity.Collections.FixedString64Bytes> _playerName =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>();

    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 20.0f;
    [SerializeField] private TextMesh playerNameTextMesh;
    
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        _playerName.OnValueChanged += OnChangePlayerName;
        
        // 先に接続済みのプレイヤーオブジェクトはplayerNameがセットされているので代入する。またOnValueChangedは実行されない。
        playerNameTextMesh.text = _playerName.Value.Value;
    }

    [Unity.Netcode.ServerRpc]
    private void SetMoveInputServerRPc(float x, float y)
    {
        _moveInput = new Vector2(x, y);
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            SetMoveInputServerRPc(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        if (IsServer)
        {
            var moveVector = new Vector3(_moveInput.x, 0, _moveInput.y);
            if (moveVector.magnitude > 1)
            {
                moveVector.Normalize();
            }

            var coefficient = (moveSpeed * moveVector.magnitude - _rigidbody.velocity.magnitude) / Time.fixedDeltaTime;

            _rigidbody.AddForce(moveVector * coefficient);

            // 移動量が0の時は回転計算をしない。方向がリセットされるため。
            if (coefficient > 0)
            {
                transform.localRotation = Quaternion.Lerp(
                    transform.localRotation,
                    Quaternion.LookRotation(moveVector),
                    rotationSpeed * Time.deltaTime
                );
            }

            _animator.SetBool("Running", coefficient > 0);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        if (IsServer)
        {
            if (col.gameObject.CompareTag("OutArea"))
            {
                MoveToStartPosition();
            }
            if (col.gameObject.CompareTag("EndFloor"))
            {
                var scoreBoard = GameObject.FindWithTag("ScoreBoard").GetComponent<ScoreBoard>();
                scoreBoard.IncrementClientScore(OwnerClientId, _playerName.Value.Value, 1);
                MoveToStartPosition();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("OnNetworkSpawn IsServer");
            MoveToStartPosition();
        }

        if (IsOwner)
        {
            Debug.Log("OnNetworkSpawn IsOwner");
            var camera = Camera.main.GetComponent<PlayerFollowCamera>();
            camera.Player = transform;

            var gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            SetPlayerNameServerRpc(gameManager.PlayerName);
        }
    }

    [Unity.Netcode.ServerRpc(RequireOwnership = true)]
    private void SetPlayerNameServerRpc(string playerName)
    {
        _playerName.Value = playerName;
    }
    
    void OnChangePlayerName(Unity.Collections.FixedString64Bytes prev, Unity.Collections.FixedString64Bytes current)
    {
        Debug.Log("OnChangePlayerName");
        if (playerNameTextMesh != null)
        {
            playerNameTextMesh.text = current.Value;
        }
    }

    private void MoveToStartPosition()
    {
        transform.position = new Vector3(Random.Range(-4f, 4f), 0.5f, -1);
    }
}