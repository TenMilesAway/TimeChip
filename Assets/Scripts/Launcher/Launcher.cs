using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TimeChip.Save;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制启动菜单、加载流程及主菜单切换
/// </summary>
public class Launcher : SingletonMono<Launcher>
{
    /// <summary>
    /// 唯一玩家存档使用的固定槽位编号
    /// </summary>
    private const int PlayerSaveSlotId = 0;

    [SerializeField] private Button _newGameButton;               // 新游戏按钮
    [SerializeField] private Button _loadSaveButton;              // 读取存档按钮
    [SerializeField] private GameObject _menuRoot;                // 启动菜单根节点
    [SerializeField] private GameObject _loadingRoot;             // 加载界面根节点

    private LauncherProcess _process = LauncherProcess.None;      // 当前 Launcher 状态
    private bool _isInitializingData;                             // 是否正在初始化数据
    private bool _isNewGame;                                     // 本次启动是否来自新建存档
    private GameSaveData _gameSaveData;                           // 当前加载的唯一存档

    protected override void Awake()
    {
        base.Awake();

        InitializeGameManager();
        MissionAPI.GameOverRequested += ShowGameOverPanel;
    }

    private void Start()
    {
        _loadingRoot.SetActive(false);
        if (_newGameButton == null || _loadSaveButton == null)
        {
            Debug.LogError("请在 Launcher 的 Inspector 中绑定新游戏按钮和读取存档按钮", this);
            return;
        }

        _newGameButton.onClick.AddListener(CreateNewGame);
        _loadSaveButton.onClick.AddListener(LoadSavedGame);
        _loadSaveButton.interactable = PlayerPrefsSaveSystem.Exists(PlayerSaveSlotId);
    }

    private void OnDestroy()
    {
        if (_newGameButton != null)
        {
            _newGameButton.onClick.RemoveListener(CreateNewGame);
        }

        if (_loadSaveButton != null)
        {
            _loadSaveButton.onClick.RemoveListener(LoadSavedGame);
        }

        PlayerInfoManager.GetInstance().PlayerInfoChanged -= SaveCurrentPlayerInfo;
        MissionAPI.GameOverRequested -= ShowGameOverPanel;
    }

    private void Update()
    {
        switch (_process)
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
                if (!_isInitializingData)
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
                _loadingRoot.SetActive(false);
                gameObject.SetActive(false);
                SetProcessState(LauncherProcess.None);
                break;
        }
    }

    /// <summary>
    /// 创建默认玩家数据并覆盖唯一存档后开始游戏
    /// </summary>
    private void CreateNewGame()
    {
        if (_process != LauncherProcess.None)
        {
            return;
        }

        _gameSaveData = CreateDefaultGameSaveData();
        SaveGameData();
        BeginLaunch(_gameSaveData, true);
    }

    /// <summary>
    /// 读取唯一存档并使用其中的玩家数据开始游戏
    /// </summary>
    private void LoadSavedGame()
    {
        if (_process != LauncherProcess.None)
        {
            return;
        }

        if (!PlayerPrefsSaveSystem.TryLoad(
                PlayerSaveSlotId,
                out GameSaveData saveData,
                out int schemaVersion))
        {
            Debug.LogWarning("未找到可读取的玩家存档", this);
            _loadSaveButton.interactable = false;
            return;
        }

        if (saveData.playerInfo == null)
        {
            Debug.LogWarning("玩家存档缺少玩家数据, 无法读取", this);
            return;
        }

        _gameSaveData = saveData;
        BeginLaunch(_gameSaveData, false);
    }

    /// <summary>
    /// 使用指定存档初始化玩家数据并进入加载流程
    /// </summary>
    /// <param name="saveData">要用于本次游戏的存档数据</param>
    /// <param name="isNewGame">是否由本次操作新建存档</param>
    private void BeginLaunch(GameSaveData saveData, bool isNewGame)
    {
        InitializePlayerInfo(saveData, isNewGame);
        _menuRoot.SetActive(false);
        _loadingRoot.SetActive(true);
        SetProcessState(LauncherProcess.PreloadBegin);
    }

    /// <summary>
    /// 异步初始化数据
    /// </summary>
    private async void InitializeDataAsync()
    {
        _isInitializingData = true;

        // 先让加载界面完成一帧渲染，再执行后续异步加载任务
        await Task.Yield();
        await LoadRequiredDataAsync();

        _isInitializingData = false;
        SetProcessState(LauncherProcess.InitDataEnd);
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private Task LoadRequiredDataAsync()
    {
        DataTableMananger.GetInstance().Init();
        MissionAPI.Initialize(PlayerInfoManager.GetInstance(), _isNewGame);
        _isNewGame = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 打开主界面及其他面板
    /// </summary>
    private void OpenMainMenu()
    {
        UIManager.GetInstance().OpenPanel(GlobalDefine.MainMenuView);
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommonTipView);
    }

    /// <summary>
    /// 使用存档中的玩家数据初始化玩家数据管理器并启用自动保存
    /// </summary>
    /// <param name="saveData">包含玩家数据的当前存档</param>
    /// <param name="isNewGame">是否由本次操作新建存档</param>
    private void InitializePlayerInfo(GameSaveData saveData, bool isNewGame)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.PlayerInfoChanged -= SaveCurrentPlayerInfo;
        playerInfoManager.Init(saveData.playerInfo);
        playerInfoManager.PlayerInfoChanged += SaveCurrentPlayerInfo;
        _isNewGame = isNewGame;
    }

    /// <summary>
    /// 创建新游戏所需的默认存档数据
    /// </summary>
    /// <returns>包含默认玩家状态的新存档</returns>
    private static GameSaveData CreateDefaultGameSaveData()
    {
        return new GameSaveData
        {
            playerInfo = CreateDefaultPlayerInfoData()
        };
    }

    /// <summary>
    /// 创建新游戏所需的默认玩家状态
    /// </summary>
    private static PlayerInfoData CreateDefaultPlayerInfoData()
    {
        return new PlayerInfoData
        {
            currentAge = 22,
            currentMonth = 1,
            health = 100,
            maxHealth = 100,
            simulationCoins = 2000,
            timeCoins = 10,
            workedThisTurn = false
        };
    }

    /// <summary>
    /// 将管理器中的最新玩家数据写入唯一存档
    /// </summary>
    /// <param name="playerInfoManager">触发数据变化的玩家数据管理器</param>
    private void SaveCurrentPlayerInfo(PlayerInfoManager playerInfoManager)
    {
        _gameSaveData.playerInfo = playerInfoManager.GetSnapshot();
        SaveGameData();
    }

    /// <summary>
    /// 保存当前唯一游戏存档
    /// </summary>
    private void SaveGameData()
    {
        PlayerPrefsSaveSystem.Save(
            PlayerSaveSlotId,
            "玩家存档",
            _gameSaveData,
            GameSaveData.CurrentSchemaVersion);
    }

    /// <summary>任务失败时展示游戏结束面板，等待玩家确认返回</summary>
    private void ShowGameOverPanel()
    {
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonOverPanel,
            UILayer.System,
            new OpenUIParam { callback = ReturnToStartMenu });
    }

    /// <summary>玩家确认后清除存档并回到启动菜单</summary>
    private void ReturnToStartMenu()
    {
        PlayerPrefsSaveSystem.Delete(PlayerSaveSlotId);
        _gameSaveData = null;
        _isInitializingData = false;
        _isNewGame = false;
        SetProcessState(LauncherProcess.None);

        UIManager.GetInstance().ClosePanel(GlobalDefine.MainMenuView);
        UIManager.GetInstance().ClosePanel(GlobalDefine.CommunityView);
        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonTipView);

        gameObject.SetActive(true);
        _loadingRoot.SetActive(false);
        _menuRoot.SetActive(true);
        _loadSaveButton.interactable = false;
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
        _process = state;
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
