using System;
using Fusion;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private TextMeshPro _nameText;
    [SerializeField] private float _speed = 5.0f;
    [SerializeField] private float _jumpForce = 5.0f;
    [SerializeField] private float _gravity = -9.81f;

    public Network.NetworkedGameManager GameManager { get; set; }

    [Header("Networked Properties")]
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Quaternion NetworkedRotation { get; set; }
    [Networked] public NetworkBool IsCrouching { get; set; }
    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int TeamID { get; set; }
    [Networked] public NetworkBool HasRepositioned { get; set; }

    private float _verticalVelocity;
    private Vector3 _originalScale;
    private Vector3 _crouchScale = new Vector3(1, 0.5f, 1);


    #region Fusion Callbacks
    public override void Spawned()
    {
        _originalScale = this.transform.localScale;
        if (HasInputAuthority) 
        {
            RPC_SetPlayerColor(LobbyUI.LocalPlayerColor);
            RPC_SetPlayerName(LobbyUI.LocalPlayerName);
            RPC_SetPlayerTeam(LobbyUI.LocalPlayerTeam);

            Camera.main.transform.SetParent(this.transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.5f, -3f); 
            Camera.main.transform.localRotation = Quaternion.Euler(10, 0, 0);
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!HasRepositioned)
        {
            NetworkedPosition = this.transform.position;
            NetworkedRotation = this.transform.rotation;
            return;
        }

        if (!GetInput(out NetworkInputData input)) return;

        Vector3 euler = input.Rotation.eulerAngles;
        this.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        NetworkedRotation = this.transform.rotation;

        IsCrouching = input.Buttons.IsSet(Network.PlayerInputButtons.Crouch);

        Vector3 move = new Vector3(input.InputVector.x, 0, input.InputVector.y);
        move = this.transform.TransformDirection(move); 
        
        if (move.magnitude > 0.1f)
        {
            move = move.normalized;
        }

        float currentSpeed = IsCrouching ? _speed * 0.5f : _speed;
        
        bool isGrounded = this.transform.position.y <= 1.05f; 

        if (isGrounded)
        {
            _verticalVelocity = 0;
            if (input.Buttons.IsSet(Network.PlayerInputButtons.Jump))
            {
                _verticalVelocity = _jumpForce;
            }
        }
        else
        {
            _verticalVelocity += _gravity * Runner.DeltaTime;
        }

        Vector3 velocity = move.normalized * currentSpeed;
        velocity.y = _verticalVelocity;

        this.transform.position += velocity * Runner.DeltaTime;
        
        if (this.transform.position.y < 1.0f)
        {
            Vector3 pos = this.transform.position;
            pos.y = 1.0f;
            this.transform.position = pos;
        }
            
        NetworkedPosition = this.transform.position;
    }

    public override void Render()
    {
        this.transform.position = NetworkedPosition;
        this.transform.rotation = NetworkedRotation;

        if (IsCrouching)
        {
            this.transform.localScale = _crouchScale;
        }
        else
        {
            this.transform.localScale = _originalScale;
        }

        if (_meshRenderer != null && _meshRenderer.material.color != PlayerColor)
        {
            _meshRenderer.material.color = PlayerColor;
        }

        if (_nameText != null)
        {
            _nameText.text = PlayerName.ToString();
            _nameText.color = PlayerColor; 
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerColor(Color color)
    {
        if (HasStateAuthority)
        {
            this.PlayerColor = color;
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        if (HasStateAuthority)
        {
            this.PlayerName = name;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerTeam(int teamID)
    {
        if (HasStateAuthority)
        {
            this.TeamID = teamID;
            
            if (GameManager == null)
            {
                GameManager = FindFirstObjectByType<Network.NetworkedGameManager>();
            }

            if (!HasRepositioned)
            {
                Vector3 teamSpawn = Vector3.zero;
                if (GameManager != null)
                {
                    teamSpawn = GameManager.GetSpawnPosition(teamID);
                }
                else
                {
                    teamSpawn = teamID == 1 ? new Vector3(-10, 1, -10) : new Vector3(10, 1, 10);
                }
                
                this.transform.position = teamSpawn;
                this.NetworkedPosition = teamSpawn;
                HasRepositioned = true;
            }
            else
            {
                Vector3 teamSpawn = Vector3.zero;
                if (GameManager != null)
                {
                    teamSpawn = GameManager.GetSpawnPosition(teamID);
                }
                else
                {
                    teamSpawn = teamID == 1 ? new Vector3(-10, 1, -10) : new Vector3(10, 1, 10);
                }
                this.transform.position = teamSpawn;
                this.NetworkedPosition = teamSpawn;
            }
        }
    }

    #endregion
    
    #region Unity Callbacks

    private void Update()
    {
        if(!HasInputAuthority) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            var randColor = Random.ColorHSV();
            RPC_SetPlayerColor(randColor);
        }
    }
    
    #endregion
    
}
