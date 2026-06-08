using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [SerializeField] private RelayManager relay;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private Button roomBackButton;

    public bool IsConnecting { get; private set; }

    private void Awake()
    {
        if (uiRoot == null)
            uiRoot = gameObject;

        hostButton.onClick.AddListener(() => _ = Host());
        joinButton.onClick.AddListener(() => _ = Join());
    }

    private async Task Host()
    {
        if (IsConnecting)
            return;

        IsConnecting = true;
        SetButtonsInteractable(false);
        SetBackButtonInteractable(false);
        statusText.text = "Creating Relay allocation...";
        try
        {
            string code = await relay.StartHostWithRelay(4);
            statusText.text = $"HOST started. Loading game... Join Code: {code}";
            joinCodeInput.text = code;
            IsConnecting = false;
            SetBackButtonInteractable(true);
        }
        catch (System.Exception e)
        {
            statusText.text = $"Host failed: {e.Message}";
            IsConnecting = false;
            SetBackButtonInteractable(true);
            SetButtonsInteractable(true);
        }
    }

    private async Task Join()
    {
        if (IsConnecting)
            return;

        string code = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter a join code.";
            SetButtonsInteractable(true);
            SetBackButtonInteractable(true);
            return;
        }

        IsConnecting = true;
        SetButtonsInteractable(false);
        SetBackButtonInteractable(false);

        statusText.text = "Joining Relay...";
        try
        {
            await relay.StartClientWithRelay(code);
            statusText.text = "CLIENT started. Joining game...";
            IsConnecting = false;
            SetBackButtonInteractable(true);
        }
        catch (System.Exception e)
        {
            statusText.text = $"Join failed: {e.Message}";
            IsConnecting = false;
            SetBackButtonInteractable(true);
            SetButtonsInteractable(true);
        }
    }

    private void SetBackButtonInteractable(bool interactable)
    {
        if (roomBackButton != null)
            roomBackButton.interactable = interactable;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null)
            hostButton.interactable = interactable;

        if (joinButton != null)
            joinButton.interactable = interactable;
    }
}