using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制启动菜单、加载流程及主菜单切换
/// </summary>
public class Launcher : SingletonMono<Launcher>
{
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject loadingRoot;

    private LauncherProcess process = LauncherProcess.None;
    private bool isInitializingData;

    protected override void Awake()
    {
        base.Awake();
        InitializeGameManager();
    }

    private void Start()
    {
        loadingRoot.SetActive(false);
        startButton.onClick.AddListener(BeginLaunch);
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(BeginLaunch);
    }

    private void Update()
    {
        switch (process)
        {
            case LauncherProcess.PreloadBegin:
                SetProcessState(LauncherProcess.PreloadIng);
                break;
            case LauncherProcess.PreloadIng:
                SetProcessState(LauncherProcess.PreloadEnd);
                break;
            case LauncherProcess.PreloadEnd:
                SetProcessState(LauncherProcess.ConnectBegin);
                break;
            case LauncherProcess.ConnectBegin:
                SetProcessState(LauncherProcess.ConnectIng);
                break;
            case LauncherProcess.ConnectIng:
                SetProcessState(LauncherProcess.ConnectEnd);
                break;
            case LauncherProcess.ConnectEnd:
                SetProcessState(LauncherProcess.InitProgressBegin);
                break;
            case LauncherProcess.InitProgressBegin:
                SetProcessState(LauncherProcess.InitProgressIng);
                break;
            case LauncherProcess.InitProgressIng:
                SetProcessState(LauncherProcess.InitProgressEnd);
                break;
            case LauncherProcess.InitProgressEnd:
                SetProcessState(LauncherProcess.InitDataBegin);
                break;
            case LauncherProcess.InitDataBegin:
                SetProcessState(LauncherProcess.InitDataIng);
                break;
            case LauncherProcess.InitDataIng:
                if (!isInitializingData)
                {
                    InitializeDataAsync();
                }
                break;
            case LauncherProcess.InitDataEnd:
                SetProcessState(LauncherProcess.SwitchSceneBegin);
                break;
            case LauncherProcess.SwitchSceneBegin:
                SetProcessState(LauncherProcess.SwitchSceneIng);
                break;
            case LauncherProcess.SwitchSceneIng:
                OpenMainMenu();
                SetProcessState(LauncherProcess.SwitchSceneEnd);
                break;
            case LauncherProcess.SwitchSceneEnd:
                loadingRoot.SetActive(false);
                gameObject.SetActive(false);
                SetProcessState(LauncherProcess.None);
                break;
        }
    }

    private void BeginLaunch()
    {
        if (process != LauncherProcess.None)
        {
            return;
        }

        menuRoot.SetActive(false);
        loadingRoot.SetActive(true);
        SetProcessState(LauncherProcess.PreloadBegin);
    }

    private async void InitializeDataAsync()
    {
        isInitializingData = true;

        // 先让加载界面完成一帧渲染，再执行后续异步加载任务
        await Task.Yield();
        await LoadRequiredDataAsync();

        isInitializingData = false;
        SetProcessState(LauncherProcess.InitDataEnd);
    }

    private Task LoadRequiredDataAsync()
    {
        DataTableMananger.GetInstance().Init();
        foreach (cfg.Item item in DataTableMananger.GetInstance().Tables.ItemTable.DataList)
        {
            Debug.Log(item.ToString());
        }

        // 后续在此添加其他数据、资源和网络初始化任务
        return Task.CompletedTask;
    }

    private void OpenMainMenu()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.MainMenuView);
        UIManager.GetInstance().OpenPanel(GlobalDefine.LotteryView);
    }

    /// <summary>
    /// 初始化 UI 资源加载和对象池所需的管理组件
    /// </summary>
    private void InitializeGameManager()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
        }

        if (gameManager.GetComponent<ResourceComponent>() == null)
        {
            gameManager.gameObject.AddComponent<ResourceComponent>();
        }

        if (gameManager.GetComponent<TimerComponent>() == null)
        {
            gameManager.gameObject.AddComponent<TimerComponent>();
        }
    }

    /// <summary>
    /// 修改 Launcher 状态
    /// </summary>
    private void SetProcessState(LauncherProcess state)
    {
        process = state;
    }
}

/// <summary>
/// Launcher 状态枚举
/// </summary>
public enum LauncherProcess
{
    None,

    // 预加载：配置、资源目录及本地配置表等
    PreloadBegin,
    PreloadIng,
    PreloadEnd,

    // 连接服务器
    ConnectBegin,
    ConnectIng,
    ConnectEnd,

    // 初始化进度信息
    InitProgressBegin,
    InitProgressIng,
    InitProgressEnd,

    // 初始化数据
    InitDataBegin,
    InitDataIng,
    InitDataEnd,

    // 切换至主菜单
    SwitchSceneBegin,
    SwitchSceneIng,
    SwitchSceneEnd,
}
