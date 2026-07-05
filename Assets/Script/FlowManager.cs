using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        stage = 1;
    }

    [SerializeField] private TMP_Text debugerText;
    int line = 0;
    public void WriteLog(string context)
    {
        if (line >= 12) {
            debugerText.text = "";
            line = 0;
        }
        line++;
        debugerText.text += "\n" + context;
    }


    string curScene = "";
    public int stage;
    public void RequestScene(string newScene)
    {
        clearPanel.SetActive(false);
        selectPanel.SetActive(false);

        curScene = newScene;

        SceneManager.LoadScene(newScene, LoadSceneMode.Single);

    }

    [SerializeField] GameObject clearPanel;
    [SerializeField] GameObject selectPanel;
    public void Clear()
    {
        clearPanel.SetActive(true);
    }

    private void Update()
    {
        
    }


}
