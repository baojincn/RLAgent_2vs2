using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ChoosePlayer : MonoBehaviour
{
    public Button bearButton;
    public Button duckButton;
    public Button dogButton;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        bearButton = root.Q<Button>("BearButton");
        duckButton = root.Q<Button>("DuckButton");
        dogButton = root.Q<Button>("DogButton");

        bearButton.clicked += BearButtonPressed;
        duckButton.clicked += DuckButtonPressed;
        dogButton.clicked += DogButtonPressed;
    }

    void BearButtonPressed()
    {
        SceneManager.LoadScene("1vs1Game_Bear");
    }

    void DuckButtonPressed()
    {
        SceneManager.LoadScene("1vs1Game_Duck");
    }

    void DogButtonPressed()
    {
        SceneManager.LoadScene("1vs1Game_Dog");
    }
}
