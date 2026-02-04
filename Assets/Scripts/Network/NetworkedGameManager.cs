using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

namespace Network
{
    public class NetworkedGameManager : NetworkBehaviour
    {
        #region Public Variables
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private Transform[] team1SpawnPoints;
        [SerializeField] private Transform[] team2SpawnPoints;
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _timerCountText;
        #endregion
        
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
        
        private const int maxPlayers = 2;
        private const int timerBeforeStart = 3;
        private bool hasGameStarted = false;
        #region Networked Properties
        [Networked] public TickTimer RoundStartTimer { get; set; }
        #endregion

        public override void Spawned()
        {
            base.Spawned();
            NetworkSessionManager.Instance.OnPlayerJoinedEvent += OnPlayerJoined;
            NetworkSessionManager.Instance.OnPlayerLeftEvent += OnPlayerLeft;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            
            NetworkSessionManager.Instance.OnPlayerJoinedEvent -= OnPlayerJoined;
            NetworkSessionManager.Instance.OnPlayerLeftEvent -= OnPlayerLeft;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            
            _playerCountText.text = 
                $"Players: {Object.Runner.ActivePlayers.Count()}/{maxPlayers}";

            if (RoundStartTimer.IsRunning)
            {
                _timerCountText.text = RoundStartTimer.RemainingTime(Object.Runner).ToString();
            }
            else
            {
                _timerCountText.text = "";
            }

            if (RoundStartTimer.Expired(Object.Runner))
            {
                OnGameStarted();
            }
        }

        private void OnPlayerJoined(PlayerRef player)
        {
            if (!HasStateAuthority) return;
            if (NetworkSessionManager.Instance.JoinedPlayers.Count 
                >= maxPlayers)
            {
                RoundStartTimer = TickTimer.CreateFromSeconds(
                    Object.Runner,
                    timerBeforeStart);
            }
            Debug.Log($"Player {player.PlayerId} Joined");
        }
        
        private void OnPlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority) return;
            if (!_spawnedCharacters.TryGetValue(player, 
                    out NetworkObject networkObject)) return;
            Object.Runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }

        private void OnGameStarted()
        {
            Debug.Log($"Game Started");
            if (hasGameStarted) return; 
            hasGameStarted = true;
            
            foreach (var playerSpawn 
                     in NetworkSessionManager.Instance.JoinedPlayers)
            {
                var spawnPosition = new Vector3(0, 1f, 0); 
                
                var networkObject = Object.Runner.Spawn(playerPrefab, 
                    spawnPosition, Quaternion.identity, playerSpawn);
                
                var networkPlayer = networkObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.GameManager = this; 
                    if (!networkPlayer.HasRepositioned)
                    {
                        networkPlayer.NetworkedPosition = spawnPosition;
                        Debug.Log($"[DEBUG_LOG] Initialized NetworkPlayer for {playerSpawn} at {spawnPosition}");
                    }
                    else
                    {
                        Debug.Log($"[DEBUG_LOG] NetworkPlayer for {playerSpawn} already repositioned via RPC. Skipping default initialization.");
                    }
                }
                
                _spawnedCharacters.Add(playerSpawn, networkObject);
            }
        }

        public Vector3 GetSpawnPosition(int teamID)
        {
            Debug.Log($"[DEBUG_LOG] GetSpawnPosition called for TeamID: {teamID}");
            if (teamID == 1 && team1SpawnPoints != null && team1SpawnPoints.Length > 0)
            {
                Vector3 pos = team1SpawnPoints[UnityEngine.Random.Range(0, team1SpawnPoints.Length)].position;
                Debug.Log($"[DEBUG_LOG] Returning Team 1 Spawn Point: {pos}");
                return pos;
            }
            if (teamID == 2 && team2SpawnPoints != null && team2SpawnPoints.Length > 0)
            {
                Vector3 pos = team2SpawnPoints[UnityEngine.Random.Range(0, team2SpawnPoints.Length)].position;
                Debug.Log($"[DEBUG_LOG] Returning Team 2 Spawn Point: {pos}");
                return pos;
            }
            
            Vector3 fallback = teamID == 1 ? new Vector3(-10, 1, -10) : new Vector3(10, 1, 10);
            Debug.Log($"[DEBUG_LOG] Returning Fallback Spawn Point for Team {teamID}: {fallback}");
            return fallback;
        }
    }
}