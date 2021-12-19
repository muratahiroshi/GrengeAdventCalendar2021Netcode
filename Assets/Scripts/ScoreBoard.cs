using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

public class ScoreBoard : NetworkBehaviour
{
    private struct ClientInfo
    {
        public ulong ClientId;
        public string PlayerName;
        public int Score;
    }
    private Dictionary<ulong, ClientInfo> _clientScoreTable = new Dictionary<ulong, ClientInfo>();
    private NetworkVariable<Unity.Collections.FixedString64Bytes> _scoreInfo =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>();

    public void IncrementClientScore(ulong clientId, string playerName, int amount)
    {
        Debug.Log("IncrementClientScore");
        if (_clientScoreTable.TryGetValue(key: clientId, out var value) == true)
        {
            value.Score += amount;
            _clientScoreTable[clientId] = value;
        }
        else
        {
            _clientScoreTable.Add(clientId, 
                new ClientInfo(){ClientId = clientId,PlayerName = playerName, Score = amount}
            );
        }
        
        var scoreInfo = "";
        foreach ( var (_, clientInfo)  in _clientScoreTable.OrderByDescending( c => c.Value.Score ) )
        {
            scoreInfo += string.Format("{0} : {1}周クリア\n", clientInfo.PlayerName, clientInfo.Score);
        }
        _scoreInfo.Value = scoreInfo;
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 60, 150, 200));
        GUILayout.Label("Score\n" + _scoreInfo.Value.Value);
        GUILayout.EndArea();
    }
}
