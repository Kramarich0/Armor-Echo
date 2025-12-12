using UnityEngine;

public static class CurrencyManager
{
    private const string CURRENCY_KEY = "PlayerStars";

    public static int GetBalance()
    {
        return PlayerPrefs.GetInt(CURRENCY_KEY, 0);
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        int current = GetBalance();
        PlayerPrefs.SetInt(CURRENCY_KEY, current + amount);
        PlayerPrefs.Save();
    }

    public static bool TrySpend(int cost)
    {
        int current = GetBalance();
        if (current >= cost)
        {
            PlayerPrefs.SetInt(CURRENCY_KEY, current - cost);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
}