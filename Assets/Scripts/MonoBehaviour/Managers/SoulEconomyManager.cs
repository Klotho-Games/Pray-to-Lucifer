using UnityEngine;

public class SoulEconomyManager : MonoBehaviour
{
    public static SoulEconomyManager instance;

    [Header("Soul State Costs")]
    /// <summary>
    /// This is the cost to fully charge the soul state meter. That means it's also the cost of either a Soul Blast or a Soul Zap
    /// </summary>
    public int FullSoulStateChargeCost = 100;
    public int CostPerHPHealed = 5;

    [Header("Dense Soul Costs")]
    public int ConvergingLensCost = 0;
    public int MirrorCost = 0;
    public int OneWayMirrorCost = 0;
    public int DiffractionSlateCost = 0;
    public int DivergingLensUpgradeToConvergingLensCost = 0;
    /// <summary>
    /// Costs of upgrading the Diffraction Plate from lvl 
    /// [0] 1 => 2
    /// [1] 2 => 3
    /// [2] 3 => 4
    /// [3] 4 => 5
    /// </summary>
    public int[] UpgradeDiffractionPlateCosts = new int[4] { 0, 0, 0, 0 };


    #region Instance
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
