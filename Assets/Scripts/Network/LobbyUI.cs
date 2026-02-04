using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;
using Network;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Dropdown colorDropdown;
    [SerializeField] private TMP_Dropdown teamDropdown;
    [SerializeField] private Button startButton;

    public static string LocalPlayerName;
    public static Color LocalPlayerColor;
    public static int LocalPlayerTeam;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        LocalPlayerName = nameInputField.text;
        if (string.IsNullOrEmpty(LocalPlayerName))
        {
            LocalPlayerName = "Player " + Random.Range(100, 999);
        }

        LocalPlayerColor = GetColorFromDropdown(colorDropdown.value);
        LocalPlayerTeam = teamDropdown.value + 1; 

        NetworkSessionManager.Instance.StartGame(GameMode.AutoHostOrClient);
        
        gameObject.SetActive(false);
    }

    private Color GetColorFromDropdown(int index)
    {
        switch (index)
        {
            case 0: return Color.red;
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            default: return Color.white;
        }
    }
}
