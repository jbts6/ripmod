public sealed class YinluQualityOverrideMap
{
    private readonly float _common;
    private readonly float _rare;
    private readonly float _epic;
    private readonly float _legend;

    public YinluQualityOverrideMap(QualityWeights weights)
    {
        _common = (float)weights.Common;
        _rare = (float)weights.Rare;
        _epic = (float)weights.Epic;
        _legend = (float)weights.Legend;
    }

    public bool TryGetValue(string key, out float value)
    {
        switch (key)
        {
            case "CommonProp":
                value = _common;
                return true;
            case "RareProp":
                value = _rare;
                return true;
            case "EpicProp":
                value = _epic;
                return true;
            case "LegendProp":
                value = _legend;
                return true;
            default:
                value = default;
                return false;
        }
    }
}
