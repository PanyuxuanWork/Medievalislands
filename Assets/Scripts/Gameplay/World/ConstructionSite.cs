/***************************************************************************
// File       : ConstructionSite.cs
// Purpose    : 单文件双模式建造
//              - Instant: 全局扣料 + 计时完工（兼容你的老流程）
//              - LogisticsAndWorkers: 物流拉料 + 工人施工（Move→BuildWorkTask）
// Notes      : 需要的周边：ResourceType、Inventory、TLog、TaskSequence、MoveToTask、BuildWorkTask、
//              IConsumer、IWorksite、CityEconomy、BuildingBase、CityContext、WarehouseBuilding、ProductionBuilding、
//              LogisticsRequestDispatcher、TaskManager。
// ***************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BuildMode { Instant, LogisticsAndWorkers }

/***************************************************************************
// File       : ConstructionSite.cs
// Purpose    : 单文件双模式建造
//              - Instant: 全局扣料 + 计时完工（兼容你的老流程）
//              - LogisticsAndWorkers: 物流拉料 + 工人施工（Move→BuildWorkTask）
// ***************************************************************************/


[DisallowMultipleComponent]
public class ConstructionSite : MonoBehaviour, IConsumer, IWorksite
{
    [Header("目标建筑")]
    public GameObject targetPrefab;            // 完工后替换成这个
    public bool autoEnableProduction = true;   // 完工时启用生产脚本（如 ProductionBuilding）

    [Header("模式")]
    public BuildMode Mode = BuildMode.Instant;

    // --------------------- Instant 模式参数 ---------------------
    [Header("Instant 模式")]
    [Tooltip("Instant 模式下的总施工时间（秒）")]
    public float instantBuildSeconds = 4f;

    [Tooltip("Instant 模式所需材料（从全局经济扣除）")]
    public ConstructionRecipe instantRecipe; // 可选：为空则不扣料仅计时

    private float _instantProgress;

    // ----------------- Logistics & Workers 模式参数 -----------------
    [Header("Logistics & Workers 模式")]
    public ConstructionRecipe recipe;            // 施工配方（物流模式必填）
    public Inventory inputInv = new Inventory(); // 施工材料本地库存

    [Tooltip("每次最少拉料批量")]
    public int minBatch = 2;
    [Tooltip("拉料巡检间隔（秒）")]
    public float checkInterval = 1.5f;
    [Tooltip("同资源下单冷却（秒）")]
    public float cooldownPerType = 4f;

    [Tooltip("总工时（为空则用 recipe.buildSeconds）")]
    public float totalWorkSeconds = 0f;
    [Tooltip("每次派发的工时块（秒）")]
    public float workChunkSeconds = 4f;
    [Tooltip("同时最多多少条施工任务在途")]
    public int maxConcurrentWorkers = 2;
    [Tooltip("派发给 TaskManager 的优先级")]
    public int taskPriority = 0;

    // 1) 兼容：Asset / City / 占位与旋转
    public BuildingAsset Asset;                // 仅作占位/信息，实际逻辑仍以 targetPrefab/recipe 为准
    public CityContext City;                // 与 _city 同源；Start() 里若 _city==null 可赋值 _city=City;
    public List<Vector2Int> OccupiedCells = new List<Vector2Int>();
    public int RotationIndex = 0;

    public float BuildTimeSeconds
    {
        get => instantBuildSeconds;
        set => instantBuildSeconds = value;
    }

    // 运行态（公共）
    private BuildingBase _bb;
    private CityContext _city;

    // 运行态（Instant）
    private bool _instantCostPaid;

    // 运行态（Logistics）
    private LogisticsRequestDispatcher _dispatcher;
    private TaskManager _tm;
    private float _nextCheck;
    private readonly Dictionary<ResourceType, float> _nextAllowed =
        new Dictionary<ResourceType, float>();
    private int _inFlight = 0;            // 在途工时块数量
    private float _accumulatedWork = 0f;  // 已完成工时

    // ------------------------------------------------------------

    void Awake()
    {
        _bb = GetComponent<BuildingBase>();
    }

    void Start()
    {
        _city = FindObjectOfType<CityContext>();
        if (_bb != null) _bb.state = BuildingState.Constructing;

        if (targetPrefab == null)
        {
            TLog.Error(this, "ConstructionSite 缺少 targetPrefab。");
            enabled = false; return;
        }

        if (Mode == BuildMode.Instant)
        {
            TLog.Log(this, "[施工] 模式=Instant（全局扣料 + 计时）", LogColor.Cyan);
            // 可选：先从全城仓库扣料
            if (instantRecipe != null && instantRecipe.costs != null && instantRecipe.costs.Length > 0)
            {
                _instantCostPaid = TryConsumeGlobalCosts(instantRecipe);
                if (!_instantCostPaid)
                {
                    TLog.Warning(this, "[施工·Instant] 全局扣料失败，仍按计时完工（如需严格，可改为取消建造）。");
                }
            }
            else
            {
                _instantCostPaid = true; // 无配方则视作不需要扣料
            }
            _instantProgress = 0f;
        }
        else // LogisticsAndWorkers
        {
            _dispatcher = FindObjectOfType<LogisticsRequestDispatcher>();
            _tm = FindObjectOfType<TaskManager>();

            if (recipe == null)
            {
                TLog.Error(this, "[施工] 模式=LogisticsAndWorkers 但未配置 recipe。");
                enabled = false; return;
            }
            if (_tm == null)
            {
                TLog.Error(this, "[施工] 找不到 TaskManager。");
                enabled = false; return;
            }

            if (totalWorkSeconds <= 0f)
                totalWorkSeconds = Mathf.Max(recipe.buildSeconds, 1f);

            _nextCheck = Time.time + Random.Range(0f, 0.3f);
            TLog.Log(this, $"[施工] 模式=LogisticsAndWorkers，总工时={totalWorkSeconds}s", LogColor.Cyan);

            if (_city == null && City != null) _city = City;
            if (targetPrefab == null && Asset != null && Asset.BuildPrefab != null) targetPrefab = Asset.BuildPrefab;
        }
    }

    void Update()
    {
        if (Mode == BuildMode.Instant)
        {
            UpdateInstant();
        }
        else
        {
            UpdateLogistics();
        }
    }

    // ========================= Instant 分支 =========================
    private void UpdateInstant()
    {
        _instantProgress += Time.deltaTime;
        if (_instantProgress >= Mathf.Max(0.1f, instantBuildSeconds))
        {
            CompleteConstruction();
        }
    }

    private bool TryConsumeGlobalCosts(ConstructionRecipe cr)
    {
        if (_city == null) { TLog.Warning(this, "[施工·Instant] 缺少 CityContext。"); return false; }
        if (cr == null || cr.costs == null || cr.costs.Length == 0) return true;

        // 将 ConstructionRecipe 转为 List<ResourceCost>
        var list = new List<ResourceCost>(cr.costs.Length);
        for (int i = 0; i < cr.costs.Length; i++)
        {
            var c = cr.costs[i];
            list.Add(new ResourceCost { Type = c.type, Amount = c.amount });
        }

        // CityEconomy 是静态工具类：从全城仓库扣料
        bool ok = CityEconomy.TryConsume(_city, list);
        return ok;
    }

    // =================== Logistics & Workers 分支 ===================
    private void UpdateLogistics()
    {
        // 1) 拉料
        if (Time.time >= _nextCheck)
        {
            _nextCheck = Time.time + checkInterval;
            TryAutoReorderCosts();
        }

        // 2) 材料齐 → 派工
        if (AllCostsMet() && NeedsWork)
        {
            TryDispatchWorkTasks();
        }

        // 3) 完工
        if (!NeedsWork)
        {
            CompleteConstruction();
        }
    }

    private void TryDispatchWorkTasks()
    {
        while (_inFlight < Mathf.Max(1, maxConcurrentWorkers) && NeedsWork)
        {
            var seq = TaskSequence.Create(
                MoveToTask.Create(transform.position),
                BuildWorkTask.Create(this, Mathf.Min(workChunkSeconds, WorkRemaining), taskPriority)
            );
            _tm.Enqueue(seq);
            _inFlight++;
            TLog.Log(this, $"[施工·派工] 发布工时块 {workChunkSeconds:F1}s，inFlight={_inFlight}", LogColor.Yellow);
        }
    }

    private void TryAutoReorderCosts()
    {
        if (_dispatcher == null || recipe == null || recipe.costs == null) return;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            var c = recipe.costs[i];
            int have = inputInv.Get(c.type);
            if (have >= c.amount) continue;

            float allowedAt;
            if (_nextAllowed.TryGetValue(c.type, out allowedAt) && Time.time < allowedAt) continue;

            int need = c.amount - have;
            int batch = Mathf.Clamp(need, Mathf.Max(1, minBatch), c.amount);

            var req = new LogisticsRequest
            {
                kind = RequestKind.PullInput,
                type = c.type,
                consumer = this,              // 工地作为 IConsumer
                quantity = batch,
                minBatch = Mathf.Max(1, minBatch),
                priority = 2
            };
            _dispatcher.Enqueue(req);
            _nextAllowed[c.type] = Time.time + cooldownPerType;
            TLog.Log(this, $"[施工·拉料] 申请 {c.type} x{batch}", LogColor.Cyan);
        }
    }

    // ========================= 完工通用流程 =========================
    private void CompleteConstruction()
    {
        var pos = transform.position;
        var rot = transform.rotation;
        var parent = transform.parent;

        var built = Instantiate(targetPrefab, pos, rot, parent);

        // 注册进 City 列表
        if (_city != null)
        {
            if (built.TryGetComponent(out WarehouseBuilding wh)) _city.warehouses.Add(wh);
            if (built.TryGetComponent(out ProductionBuilding pb)) _city.productions.Add(pb);
        }

        // 状态切到 Active
        if (built.TryGetComponent(out BuildingBase bb))
        {
            bb.state = BuildingState.Active;
        }

        // 自动启用生产
        if (autoEnableProduction && built.TryGetComponent(out ProductionBuilding prod))
        {
            prod.enabled = true;
        }

        TLog.Log(this, $"[施工] 完工 → {built.name} 已激活。", LogColor.Green);
        Destroy(gameObject);
    }

    // ========================= IWorksite 实现 =========================
    public bool NeedsWork
    {
        get
        {
            if (Mode == BuildMode.Instant) return _instantProgress < Mathf.Max(0.1f, instantBuildSeconds);
            return _accumulatedWork < Mathf.Max(1f, totalWorkSeconds);
        }
    }

    public float WorkRemaining
    {
        get
        {
            if (Mode == BuildMode.Instant) return Mathf.Max(0f, Mathf.Max(0.1f, instantBuildSeconds) - _instantProgress);
            return Mathf.Max(0f, Mathf.Max(1f, totalWorkSeconds) - _accumulatedWork);
        }
    }

    public bool CanStartWork()
    {
        if (Mode == BuildMode.Instant) return true; // 即时模式随时可推进计时
        return AllCostsMet();
    }

    public void AddWork(float workAmount)
    {
        if (Mode == BuildMode.Instant) return; // 即时模式不需要工人“加工时”
        if (!CanStartWork() || !NeedsWork) return;
        _accumulatedWork += Mathf.Max(0f, workAmount);
        if (_accumulatedWork > totalWorkSeconds) _accumulatedWork = totalWorkSeconds;

        if (Random.value < 0.06f)
            TLog.Log(this, $"[施工进度] {_accumulatedWork:F1}/{totalWorkSeconds:F1}", LogColor.Cyan);

        if (_inFlight > 0 && WorkRemaining <= (workChunkSeconds * 0.5f))
            _inFlight = Mathf.Max(0, _inFlight - 1);
    }

    // ========================= IConsumer 实现（按你的接口） =========================
    public bool CanAccept(ResourceType type, int amount)
    {
        if (Mode == BuildMode.Instant) return false; // 即时模式不接物流材料
        float target = GetNeed(type);
        if (target <= 0) return false;               // 非配方材料不接收
        int have = inputInv.Get(type);
        return have < target;                        // 只在未满足上限时接收
    }

    public bool TryAccept(ResourceType type, int amount)
    {
        if (Mode == BuildMode.Instant) return false;
        if (!CanAccept(type, amount)) return false;

        int target = (int)GetNeed(type);
        int have = inputInv.Get(type);
        int room = Mathf.Max(0, target - have);
        int take = Mathf.Min(room, amount);
        if (take <= 0) return false;

        inputInv.Add(type, take);
        TLog.Log(this, $"[施工·收料] {type} +{take} (现有 {inputInv.Get(type)}/{target})", LogColor.Yellow);
        return true;
    }

    // ------------------------ 辅助 ------------------------
    private float GetNeed(ResourceType t)
    {
        if (recipe == null || recipe.costs == null) return 0;
        for (int i = 0; i < recipe.costs.Length; i++)
            if (recipe.costs[i].type.Equals(t)) return recipe.costs[i].amount;
        return 0;
    }

    private bool AllCostsMet()
    {
        if (recipe == null || recipe.costs == null) return true;
        for (int i = 0; i < recipe.costs.Length; i++)
        {
            var c = recipe.costs[i];
            if (inputInv.Get(c.type) < c.amount) return false;
        }
        return true;
    }
}
