using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TimeChip.Save;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    private const int NewGameTestBuffId = 1001;
    private const int NewLifeMemoryPointReward = 30000;
    private const int NewLifeGrowCardUnlockCount = 3;
    private const string PreloadGameContentLabel = "preload-game-content";

    [SerializeField] private Button _newGameButton;               // 新游戏按钮
    [SerializeField] private Button _loadSaveButton;              // 读取存档按钮
    [SerializeField] private Button _growButton;                  // 时光藏馆按钮
    [SerializeField] private Button _settingButton;               // 设置按钮
    [SerializeField] private GameObject _menuRoot;                // 启动菜单根节点
    [SerializeField] private GameObject _loadingRoot;             // 加载界面根节点
    [SerializeField] private Text _txtLoad;                       // 加载进度文本

    private LauncherProcess _process = LauncherProcess.None;      // 当前 Launcher 状态
    private bool _isInitializingData;                             // 是否正在初始化数据
    private bool _isInitializingTables;                           // 是否正在初始化数据表
    private bool _isPreloadingGameContent;                        // 是否正在下载进入游戏所需的 AA 资源
    private bool _isOpeningMenuPanel;                             // 是否正在通过转场打开启动菜单面板
    private bool _isNewGame;                                      // 本次启动是否来自新建存档
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
        if (_newGameButton == null || _loadSaveButton == null || _growButton == null || _settingButton == null)
        {
            Debug.LogError("请在 Launcher 的 Inspector 中绑定新游戏、读取存档、时光藏馆和设置按钮", this);
            return;
        }

        _newGameButton.onClick.AddListener(CreateNewGame);
        _loadSaveButton.onClick.AddListener(LoadSavedGame);
        _growButton.onClick.AddListener(OpenGrowView);
        _settingButton.onClick.AddListener(OpenSettingView);
        _growButton.interactable = false;
        _loadSaveButton.interactable = PlayerPrefsSaveSystem.Exists(PlayerSaveSlotId);

        InitializeTablesForGrowButtonAsync();
    }

    private async void InitializeTablesForGrowButtonAsync()
    {
        if (_isInitializingTables)
        {
            return;
        }

        _isInitializingTables = true;
        await DataTableMananger.GetInstance().Init();
        _isInitializingTables = false;

        if (_growButton == null)
        {
            return;
        }

        bool isTableReady = DataTableMananger.GetInstance().Tables != null;
        _growButton.interactable = isTableReady;
        if (!isTableReady)
        {
            Debug.LogError("数据表初始化失败，时光藏馆按钮保持禁用", this);
        }
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

        if (_growButton != null)
        {
            _growButton.onClick.RemoveListener(OpenGrowView);
        }

        if (_settingButton != null)
        {
            _settingButton.onClick.RemoveListener(OpenSettingView);
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
                if (!_isPreloadingGameContent)
                {
                    PreloadGameContentAsync();
                }
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
    /// 下载进入游戏所需的 Addressables 依赖，下载完成后才继续加载流程
    /// </summary>
    private async void PreloadGameContentAsync()
    {
        _isPreloadingGameContent = true;
        AsyncOperationHandle downloadHandle =
            Addressables.DownloadDependenciesAsync(PreloadGameContentLabel, false);

        while (!downloadHandle.IsDone)
        {
            UpdateDownloadProgress(downloadHandle, "游戏");
            await Task.Yield();
        }

        UpdateDownloadProgress(downloadHandle, "游戏");
        bool downloadSucceeded = downloadHandle.Status == AsyncOperationStatus.Succeeded;
        Addressables.Release(downloadHandle);
        _isPreloadingGameContent = false;

        if (!downloadSucceeded)
        {
            Debug.LogError(
                $"下载 Addressables 标签 \"{PreloadGameContentLabel}\" 的游戏资源失败，已取消进入游戏。",
                this);
            ReturnToStartMenuInternal(deleteSave: false);
            return;
        }

        SetProcessState(LauncherProcess.PreloadEnd);
    }

    /// <summary>
    /// 将 Addressables 的实际下载字节进度显示到加载界面
    /// </summary>
    private void UpdateDownloadProgress(AsyncOperationHandle downloadHandle, string resourceName)
    {
        if (_txtLoad == null)
        {
            return;
        }

        DownloadStatus downloadStatus = downloadHandle.GetDownloadStatus();
        int progressPercent = Mathf.FloorToInt(
            Mathf.Clamp01(downloadStatus.Percent) * 100f);
        _txtLoad.text = $"正在下载{resourceName}资源... {progressPercent}%";
    }

    /// <summary>
    /// 创建默认玩家数据并覆盖唯一存档后开始游戏
    /// </summary>
    private async void CreateNewGame()
    {
        if (_process != LauncherProcess.None || _isOpeningMenuPanel)
        {
            return;
        }

        _gameSaveData = CreateDefaultGameSaveData();
        await GrantNewLifeGrowRewardAsync();
        SaveGameData();
        BeginLaunch(_gameSaveData, true);
    }

    /// <summary>
    /// 读取唯一存档并使用其中的玩家数据开始游戏
    /// </summary>
    private void LoadSavedGame()
    {
        if (_process != LauncherProcess.None || _isOpeningMenuPanel)
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
        if (_txtLoad != null)
        {
            _txtLoad.text = "正在检查游戏资源...";
        }
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
        if (_txtLoad != null)
        {
            _txtLoad.text = "正在初始化游戏数据...";
        }

        // 先让加载界面完成一帧渲染，再执行后续异步加载任务
        await Task.Yield();
        await LoadRequiredDataAsync();

        _isInitializingData = false;
        SetProcessState(LauncherProcess.InitDataEnd);
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadRequiredDataAsync()
    {
        await DataTableMananger.GetInstance().Init();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("数据表初始化失败，无法继续加载流程", this);
            return;
        }

        GlobalInfoManager.GetInstance().Init();
        GlobalInfoManager.GetInstance().EnsureGrowCards(
            tables.GrowTable.DataMap.Keys);
        BuffSystem.GetInstance().Initialize(PlayerInfoManager.GetInstance());
        if (_isNewGame)
        {
            BuffSystem.GetInstance().TryAddBuff(NewGameTestBuffId);
        }

        MissionAPI.Initialize(PlayerInfoManager.GetInstance(), _isNewGame);
        _isNewGame = false;
    }

    /// <summary>
    /// 打开主界面及其他面板
    /// </summary>
    private void OpenMainMenu()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(GlobalDefine.MainMenuView);
        UIManager.GetInstance().OpenPanel(GlobalDefine.CommunityView);
    }

    private async void OpenSettingView()
    {
        if (!TryBeginMenuPanelTransition("设置"))
        {
            return;
        }

        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (!await DownloadMenuPanelDependenciesAsync(GlobalDefine.SettingView, "设置"))
        {
            EndMenuPanelTransition();
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.SettingView,
            action: EndMenuPanelTransition);
    }

    private async void OpenGrowView()
    {
        if (!TryBeginMenuPanelTransition("时光藏馆"))
        {
            return;
        }

        if (!await DownloadMenuPanelDependenciesAsync(GlobalDefine.GrowView, "时光藏馆"))
        {
            EndMenuPanelTransition();
            return;
        }

        await DataTableMananger.GetInstance().Init();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("数据表初始化失败，无法打开成长界面", this);
            EndMenuPanelTransition();
            return;
        }

        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        globalInfoManager.Init();
        globalInfoManager.EnsureGrowCards(
            tables.GrowTable.DataMap.Keys);
        GameManager.Audio.Play(AudioDefine.SFXClick);
        UIManager.GetInstance().OpenPanel(
            GlobalDefine.GrowView,
            action: EndMenuPanelTransition);
    }

    /// <summary>
    /// 显示启动菜单中的转场界面并防止重复打开面板
    /// </summary>
    private bool TryBeginMenuPanelTransition(string panelName)
    {
        if (_process != LauncherProcess.None || _isOpeningMenuPanel)
        {
            return false;
        }

        _isOpeningMenuPanel = true;
        if (_txtLoad != null)
        {
            _txtLoad.text = $"正在检查{panelName}资源...";
        }
        _loadingRoot.SetActive(true);
        return true;
    }

    /// <summary>
    /// 下载启动菜单目标面板的 Addressables 依赖
    /// </summary>
    private async Task<bool> DownloadMenuPanelDependenciesAsync(
        string panelAddress,
        string panelName)
    {
        AsyncOperationHandle downloadHandle =
            Addressables.DownloadDependenciesAsync(panelAddress, false);

        while (!downloadHandle.IsDone)
        {
            UpdateDownloadProgress(downloadHandle, panelName);
            await Task.Yield();
        }

        UpdateDownloadProgress(downloadHandle, panelName);
        bool downloadSucceeded = downloadHandle.Status == AsyncOperationStatus.Succeeded;
        Addressables.Release(downloadHandle);

        if (!downloadSucceeded)
        {
            Debug.LogError(
                $"下载 {panelName} 的 Addressables 资源失败，已取消打开面板。",
                this);
        }

        return downloadSucceeded;
    }

    /// <summary>
    /// 目标面板显示或下载失败后，关闭启动菜单转场界面
    /// </summary>
    private void EndMenuPanelTransition()
    {
        _isOpeningMenuPanel = false;
        _loadingRoot.SetActive(false);
    }

    private async Task GrantNewLifeGrowRewardAsync()
    {
        await DataTableMananger.GetInstance().Init();
        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            Debug.LogError("数据表初始化失败，无法发放新生奖励", this);
            return;
        }

        GlobalInfoManager globalInfoManager = GlobalInfoManager.GetInstance();
        globalInfoManager.Init();
        globalInfoManager.EnsureGrowCards(
            tables.GrowTable.DataMap.Keys);
        globalInfoManager.GrantNewLifeReward(
            NewLifeMemoryPointReward,
            NewLifeGrowCardUnlockCount);
    }

    /// <summary>保存当前存档并返回启动主界面</summary>
    public void SaveAndReturnToMainInterface()
    {
        if (_gameSaveData != null)
        {
            _gameSaveData.playerInfo = PlayerInfoManager.GetInstance().GetSnapshot();
            SaveGameData();
        }

        ReturnToStartMenuInternal(deleteSave: false);
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
            wheelCoins = 10,
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
        ReturnToStartMenuInternal(deleteSave: true);
    }

    private void ReturnToStartMenuInternal(bool deleteSave)
    {
        PlayerInfoManager.GetInstance().PlayerInfoChanged -= SaveCurrentPlayerInfo;

        if (deleteSave)
        {
            PlayerPrefsSaveSystem.Delete(PlayerSaveSlotId);
            _gameSaveData = null;
        }

        _isInitializingData = false;
        _isPreloadingGameContent = false;
        _isNewGame = false;
        SetProcessState(LauncherProcess.None);

        UIManager.GetInstance().CloseAllPanels();

        gameObject.SetActive(true);
        _loadingRoot.SetActive(false);
        _menuRoot.SetActive(true);
        _loadSaveButton.interactable = PlayerPrefsSaveSystem.Exists(PlayerSaveSlotId);
    }

    /// <summary>
    /// 初始化 UI 资源加载和对象池所需的管理组件
    /// </summary>
    private void InitializeGameManager()
    {
        // 场景中已手动创建 GameManager, 因此 return
        return;

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

        if (gameManager.GetComponent<AudioComponent>() == null)
        {
            gameManager.gameObject.AddComponent<AudioComponent>();
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
