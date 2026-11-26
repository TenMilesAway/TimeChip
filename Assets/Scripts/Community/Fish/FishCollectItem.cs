using UnityEngine;
using UnityEngine.UI;

public class FishCollectItem : MonoBehaviour
{
    private const string MedalAtlasPrefix =
        "Assets/Art/Community/SpriteAtlas.spriteatlasv2[";
    private const string BronzeMedalPath = MedalAtlasPrefix + "fish_rank_medal_bronze_128]";
    private const string SilverMedalPath = MedalAtlasPrefix + "fish_rank_medal_silver_128]";
    private const string GoldMedalPath = MedalAtlasPrefix + "fish_rank_medal_gold_128]";
    private const string PerfectMedalPath = MedalAtlasPrefix + "fish_rank_medal_purple_gold_128]";

    private static readonly Color RareLevelColor = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EpicLevelColor = new Color(0.7f, 0.3f, 1f);
    private static readonly Color LegendaryLevelColor = new Color(1f, 0.75f, 0.1f);
    private static readonly Color MythicLevelColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtType;
    [SerializeField] private Text _txtLength;
    [SerializeField] private Text _txtMaxLength;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Image _imgIconDark;
    [SerializeField] private Image _imgLevel;

    private int _loadVersion;

    public void SetData(cfg.Fish fishConfig, bool unlocked, int maxLengthCm)
    {
        _loadVersion++;
        int loadVersion = _loadVersion;
        gameObject.SetActive(fishConfig != null);
        if (fishConfig == null)
        {
            return;
        }

        if (unlocked)
        {
            _txtName.text = fishConfig.Name;
            SetQuality(fishConfig.Rarity);
            _txtLength.text = $"{fishConfig.SizeMinCm} ~ {fishConfig.SizeMaxCm} cm";
            _txtMaxLength.text = $"{maxLengthCm} cm";
            _imgIconDark.gameObject.SetActive(false);
            ApplyMedal(fishConfig, maxLengthCm, loadVersion);
        }
        else
        {
            _txtName.text = "???";
            _txtType.text = "???";
            _txtType.color = Color.black;
            _txtLength.text = "?? ~ ?? cm";
            _txtMaxLength.text = "0 cm";
            _imgIconDark.gameObject.SetActive(true);
            _imgLevel.gameObject.SetActive(false);
            _imgLevel.sprite = null;
        }

        LoadIcon(fishConfig.Icon, loadVersion);
    }

    private async void LoadIcon(string iconPath, int loadVersion)
    {
        _imgIcon.sprite = null;
        if (string.IsNullOrEmpty(iconPath) || GameManager.Resource == null)
        {
            return;
        }

        Sprite icon = await GameManager.Resource.LoadResource<Sprite>(
            iconPath,
            GetInstanceID().ToString());
        if (loadVersion != _loadVersion || icon == null)
        {
            return;
        }

        _imgIcon.sprite = icon;
        if (_imgIconDark != null)
        {
            _imgIconDark.sprite = icon;
        }
    }

    private async void ApplyMedal(cfg.Fish fishConfig, int maxLengthCm, int loadVersion)
    {
        string medalPath = GetMedalPath(fishConfig, maxLengthCm);
        if (string.IsNullOrEmpty(medalPath))
        {
            _imgLevel.sprite = null;
            _imgLevel.gameObject.SetActive(false);
            return;
        }

        _imgLevel.gameObject.SetActive(true);
        _imgLevel.sprite = null;
        if (GameManager.Resource == null)
        {
            return;
        }

        Sprite medal = await GameManager.Resource.LoadResource<Sprite>(
            medalPath,
            GetInstanceID().ToString());
        if (loadVersion != _loadVersion || medal == null)
        {
            return;
        }

        _imgLevel.sprite = medal;
    }

    private static string GetMedalPath(cfg.Fish fishConfig, int maxLengthCm)
    {
        if (fishConfig.PerfectSizeCm > 0 && maxLengthCm >= fishConfig.PerfectSizeCm)
        {
            return PerfectMedalPath;
        }

        if (fishConfig.GoldSizeCm > 0 && maxLengthCm >= fishConfig.GoldSizeCm)
        {
            return GoldMedalPath;
        }

        if (fishConfig.SilverSizeCm > 0 && maxLengthCm >= fishConfig.SilverSizeCm)
        {
            return SilverMedalPath;
        }

        if (fishConfig.BronzeSizeCm > 0 && maxLengthCm >= fishConfig.BronzeSizeCm)
        {
            return BronzeMedalPath;
        }

        return string.Empty;
    }

    private void SetQuality(int rarity)
    {
        switch (rarity)
        {
            case 2:
                _txtType.text = "稀有";
                _txtType.color = RareLevelColor;
                break;
            case 3:
                _txtType.text = "史诗";
                _txtType.color = EpicLevelColor;
                break;
            case 4:
                _txtType.text = "传说";
                _txtType.color = LegendaryLevelColor;
                break;
            case 5:
                _txtType.text = "神话";
                _txtType.color = MythicLevelColor;
                break;
            default:
                _txtType.text = "普通";
                _txtType.color = Color.white;
                break;
        }
    }
}
