using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Menu : MonoBehaviour
{
    public Button startButton;
    public Button spectatorButton;
    public Button aboutButton;

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("PlayButton");
        spectatorButton = root.Q<Button>("SpectatorButton");
        aboutButton = root.Q<Button>("AboutButton");

        startButton.clicked += PlayButtonPressed;
        spectatorButton.clicked += SpectatorButtonPressed;
        aboutButton.clicked += AboutButtonPressed;
    }

    void PlayButtonPressed()
    {
        SceneManager.LoadScene("ChooseYourCharacter");
    }

    void SpectatorButtonPressed()
    {
        SceneManager.LoadScene("Spectator");
    }

    void AboutButtonPressed()
    {
        SceneManager.LoadScene("About");
    }
}
