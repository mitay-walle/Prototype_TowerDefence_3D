using UnityEngine;
using UnityEngine.Events;

namespace TD.GameLoop
{
    public class ResourceManager : MonoBehaviour
    {
        private const string TOOLTIP_PASSIVE_INCOME = "Currency given at the end of each wave";
        private const string TOOLTIP_ENABLE_INCOME = "Whether passive income is enabled";

        [SerializeField] private bool Logs = false;
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int startingCurrency = 500;

        [Tooltip(TOOLTIP_PASSIVE_INCOME)]
        [SerializeField] private int passiveIncomePerWave = 100;
        [Tooltip(TOOLTIP_ENABLE_INCOME)]
        [SerializeField] private bool enablePassiveIncome = true;

        [SerializeField] private int currentCurrency;

        public UnityEvent<int> onCurrencyChanged;
        public UnityEvent<int> onCurrencyGained;
        public UnityEvent<int> onCurrencySpent;

        public int CurrentCurrency => currentCurrency;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentCurrency = startingCurrency;
        }

        private void Start()
        {
            onCurrencyChanged?.Invoke(currentCurrency);
        }

        public bool CanAfford(int cost)
        {
            return currentCurrency >= cost;
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
            {
                if (Logs) Debug.Log($"[ResourceManager] Cannot afford {amount}, current: {currentCurrency}");
                return false;
            }

            currentCurrency -= amount;
            if (Logs) Debug.Log($"[ResourceManager] Spent {amount}, remaining: {currentCurrency}");

            onCurrencySpent?.Invoke(amount);
            onCurrencyChanged?.Invoke(currentCurrency);
            return true;
        }

        public void AddCurrency(int amount)
        {
            currentCurrency += amount;
            if (Logs) Debug.Log($"[ResourceManager] Gained {amount}, total: {currentCurrency}");

            onCurrencyGained?.Invoke(amount);
            onCurrencyChanged?.Invoke(currentCurrency);
        }

        public void GivePassiveIncome()
        {
            if (enablePassiveIncome)
            {
                AddCurrency(passiveIncomePerWave);
            }
        }

        public void Reset()
        {
            currentCurrency = startingCurrency;
            onCurrencyChanged?.Invoke(currentCurrency);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            onCurrencyChanged?.RemoveAllListeners();
            onCurrencyGained?.RemoveAllListeners();
            onCurrencySpent?.RemoveAllListeners();
        }
    }
}
