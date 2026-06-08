using System.Threading.Tasks;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private bool loadGameplaySceneOnHost = true;
    [SerializeField] private string gameplaySceneName = "Test_Scene";

    private async void Awake()
    {
        if (networkManager == null) networkManager = FindFirstObjectByType<NetworkManager>();
        if (unityTransport == null) unityTransport = FindFirstObjectByType<UnityTransport>();

        await EnsureServicesSignedIn();
    }

    private static async Task EnsureServicesSignedIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // HOST: creates allocation + join code, configures transport, starts host
    public async Task<string> StartHostWithRelay(int maxConnections = 4)
    {
        await EnsureServicesSignedIn();

        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        RelayServerEndpoint endpoint = alloc.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        unityTransport.SetHostRelayData(
            endpoint.Host,
            (ushort)endpoint.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            endpoint.Secure
        );

        bool started = networkManager.StartHost();
        if (!started)
            throw new System.InvalidOperationException("Failed to start host.");

        if (loadGameplaySceneOnHost)
            LoadGameplaySceneAsHost();

        return joinCode;
    }

    // CLIENT: joins by join code, configures transport, starts client
    public async Task StartClientWithRelay(string joinCode)
    {
        await EnsureServicesSignedIn();

        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        RelayServerEndpoint endpoint = joinAlloc.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        unityTransport.SetClientRelayData(
            endpoint.Host,
            (ushort)endpoint.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData,
            endpoint.Secure
        );

        bool started = networkManager.StartClient();
        if (!started)
            throw new System.InvalidOperationException("Failed to start client.");
    }

    private void LoadGameplaySceneAsHost()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
            return;

        if (networkManager != null && networkManager.NetworkConfig != null && networkManager.NetworkConfig.EnableSceneManagement && networkManager.SceneManager != null)
        {
            networkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
            return;
        }

        Debug.LogWarning("Netcode scene management is disabled. Loading scene locally on host only.");
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }
}