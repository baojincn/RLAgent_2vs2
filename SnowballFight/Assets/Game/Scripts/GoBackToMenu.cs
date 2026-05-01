using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GoBackToMenu : MonoBehaviour
{
    public Button GoBackToMenuButton;

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        GoBackToMenuButton = root.Q<Button>("GoBackToMenuButton");
        GoBackToMenuButton.clicked += GoBackToMenuButtonPressed;
    }

    void GoBackToMenuButtonPressed()
    {
        SceneManager.LoadScene("GameMenu");
    }
}
