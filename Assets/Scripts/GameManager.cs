using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject moveXFloorPrefab;
    [SerializeField] private GameObject scoreBoardPrefab;

    private string _textIpAddress = "127.0.0.1";
    private string _port = "7777";
    private string _playerName = "プレイヤー名";

    public string PlayerName
    {
        get => _playerName;
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
#if UNITY_SERVER
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("StartServer");
            StartServer();
        }
#endif
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 150, 220));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    private void StartButtons()
    {
        if (GUILayout.Button("Server"))
        {
            StartServer();
        }

        if (GUILayout.Button("Host"))
        {
            StartHost();
        }

        if (GUILayout.Button("Client"))
        {
            StartClient(_textIpAddress.Trim(), Convert.ToUInt16(_port));
        }

        GUILayout.Label("IpAddress");
        _textIpAddress = GUILayout.TextField(_textIpAddress);

        GUILayout.Label("Port");
        _port = GUILayout.TextField(_port);

        GUILayout.Label("PlayerName");
        _playerName = GUILayout.TextField(_playerName);
    }

    private void StartServer()
    {
        NetworkManager.Singleton.OnServerStarted += OnStartServer;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport.Initialize();
        NetworkManager.Singleton.StartServer();
    }

    private void StartHost()
    {
        NetworkManager.Singleton.OnServerStarted += OnStartServer;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport.Initialize();
        NetworkManager.Singleton.StartHost();
    }

    private void StartClient(string ipAddress, ushort port)
    {
        var transport = Unity.Netcode.NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        if (transport is Unity.Netcode.UnityTransport)
        {
            var unityTransport = transport as Unity.Netcode.UnityTransport;
            if (unityTransport != null)
            {
                unityTransport.SetConnectionData(ipAddress, port);
            }
        }

        if (transport is Unity.Netcode.Transports.UNET.UNetTransport)
        {
            var unetTransport = transport as Unity.Netcode.Transports.UNET.UNetTransport;
            if (unetTransport != null)
            {
                unetTransport.ConnectAddress = ipAddress;
                unetTransport.ConnectPort = port;
            }
        }

        NetworkManager.Singleton.StartClient();
    }

    private void SpawnMoveXFloorPrefab(Vector3 position, Vector3 scale, int inverseCounter = 1500,
        float move = 0.002f, float initialDirection = 1.0f)
    {
        var gmo = GameObject.Instantiate(moveXFloorPrefab, position, Quaternion.identity);
        gmo.transform.localScale = scale;

        var moveXFloorObject = gmo.GetComponent<MoveXFloor>();
        moveXFloorObject.inverseCounter = inverseCounter;
        moveXFloorObject.move = move;
        moveXFloorObject.initialDirection = initialDirection;

        var netObject = gmo.GetComponent<NetworkObject>();
        netObject.Spawn(true);
    }

    private void OnStartServer()
    {
        Debug.Log("OnStartServer");

        // 動く障害物を生成
        SpawnMoveXFloorPrefab(
            new Vector3(-2.5f, 1.0f, 10.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            90,
            0.05f
        );
        SpawnMoveXFloorPrefab(
            new Vector3(1.5f, 1.0f, 9.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            250,
            0.01f,
            -1.0f
        );

        SpawnMoveXFloorPrefab(
            new Vector3(-2.5f, 1.0f, 6.5f),
            new Vector3(1.0f, 1.0f, 1.0f),
            250,
            0.01f
        );

        SpawnMoveXFloorPrefab(
            new Vector3(1.5f, 1.0f, 4.5f),
            new Vector3(1.0f, 1.0f, 1.0f),
            250,
            0.016f
        );

        SpawnMoveXFloorPrefab(
            new Vector3(-4.0f, 1.0f, 2.5f),
            new Vector3(1.0f, 1.0f, 1.0f),
            100,
            0.04f
        );

        SpawnMoveXFloorPrefab(
            new Vector3(0.5f, 1.0f, 1.5f),
            new Vector3(1.0f, 1.0f, 1.0f),
            200,
            0.015f
        );
        
        // スコア表示
        var gmo = GameObject.Instantiate(scoreBoardPrefab);
        var netObject = gmo.GetComponent<NetworkObject>();
        netObject.Spawn(true);
    }

    private void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ? "Host" :
            NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}