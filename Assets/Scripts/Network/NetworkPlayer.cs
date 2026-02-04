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

    public Network.NetworkedGameManager GameManager { get; set; }

    [Header("Networked Properties")]
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int TeamID { get; set; }
    [Networked] public NetworkBool HasRepositioned { get; set; }


    #region Fusion Callbacks
    public override void Spawned()
    {
        Debug.Log($"[DEBUG_LOG] Player Spawned: ID={Object.InputAuthority}, HasInputAuthority={HasInputAuthority}, HasStateAuthority={HasStateAuthority}");
        if (HasInputAuthority) 
        {
            Debug.Log($"[DEBUG_LOG] Calling RPCs for Player {Object.InputAuthority}: Team={LobbyUI.LocalPlayerTeam}");
            RPC_SetPlayerColor(LobbyUI.LocalPlayerColor);
            RPC_SetPlayerName(LobbyUI.LocalPlayerName);
            RPC_SetPlayerTeam(LobbyUI.LocalPlayerTeam);

            Camera.main.transform.SetParent(this.transform);
            Camera.main.transform.localPosition = new Vector3(0, 10, -10); 
            Camera.main.transform.localRotation = Quaternion.Euler(45, 0, 0);
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
            return;
        }

        if (!GetInput(out NetworkInputData input)) return;
        
        
        this.transform.position +=
            new Vector3(input.InputVector.normalized.x,
                0,
                input.InputVector.normalized.y)
            * Runner.DeltaTime * _speed;
            
            
        NetworkedPosition = this.transform.position;
    }

    public override void Render()
    {
        this.transform.position = NetworkedPosition;

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
        Debug.Log($"[DEBUG_LOG] RPC_SetPlayerTeam received on Server for Player {Object.InputAuthority}: teamID argument={teamID}");
        if (HasStateAuthority)
        {
            this.TeamID = teamID;
            Debug.Log($"[DEBUG_LOG] Player {Object.InputAuthority} TeamID set to {this.TeamID}");
            
            if (GameManager == null)
            {
                GameManager = FindFirstObjectByType<Network.NetworkedGameManager>();
                Debug.Log($"[DEBUG_LOG] GameManager was null, search result: {GameManager != null}");
            }

            if (!HasRepositioned)
            {
                Vector3 teamSpawn = Vector3.zero;
                if (GameManager != null)
                {
                    teamSpawn = GameManager.GetSpawnPosition(teamID);
                    Debug.Log($"[DEBUG_LOG] Got spawn position from GameManager for team {teamID}: {teamSpawn}");
                }
                else
                {
                    teamSpawn = teamID == 1 ? new Vector3(-10, 1, -10) : new Vector3(10, 1, 10);
                    Debug.Log($"[DEBUG_LOG] GameManager not found. Using local fallback spawn position for team {teamID}: {teamSpawn}");
                }
                
                this.transform.position = teamSpawn;
                this.NetworkedPosition = teamSpawn;
                HasRepositioned = true;
                Debug.Log($"[DEBUG_LOG] Repositioned Player {Object.InputAuthority} to Team {teamID} at {teamSpawn}. NetworkedPosition is now {NetworkedPosition}");
            }
            else
            {
                Debug.Log($"[DEBUG_LOG] Player {Object.InputAuthority} already repositioned. Re-calculating position anyway to ensure consistency.");
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
                Debug.Log($"[DEBUG_LOG] Force-repositioned Player {Object.InputAuthority} to Team {teamID} at {teamSpawn}");
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
