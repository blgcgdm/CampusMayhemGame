using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Aktif maçın isimleri ve tüm maçların birikmiş kazanma sayıları.
// İsimler sahne geçişinde yaşamalı, o yüzden statik. Tablo uygulama
// kapansa da kalmalı, o yüzden PlayerPrefs. PlayerPrefs anahtarları
// listelenemediği için isim listesi ayrıca tutuluyor.
public static class PlayerProfiles
{
    const string WinPrefix = "wins:";
    const string RosterKey = "roster";
    const char Separator = '\n';

    static readonly string[] names = { "OYUNCU 1", "OYUNCU 2" };

    public static string Name(int playerIndex)
    {
        return names[Mathf.Clamp(playerIndex - 1, 0, names.Length - 1)];
    }

    public static void SetNames(string first, string second)
    {
        names[0] = Clean(first, "OYUNCU 1");
        names[1] = Clean(second, "OYUNCU 2");
    }

    public static void RecordWin(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim();
        PlayerPrefs.SetInt(WinPrefix + name, WinsOf(name) + 1);

        var roster = Roster();
        if (!roster.Contains(name))
        {
            roster.Add(name);
            PlayerPrefs.SetString(RosterKey, string.Join(Separator.ToString(), roster));
        }

        PlayerPrefs.Save();
    }

    public static int WinsOf(string name)
    {
        return PlayerPrefs.GetInt(WinPrefix + name, 0);
    }

    public static List<KeyValuePair<string, int>> Standings()
    {
        return Roster()
            .Select(n => new KeyValuePair<string, int>(n, WinsOf(n)))
            .OrderByDescending(e => e.Value)
            .ThenBy(e => e.Key)
            .ToList();
    }

    public static void Reset()
    {
        foreach (string n in Roster()) PlayerPrefs.DeleteKey(WinPrefix + n);
        PlayerPrefs.DeleteKey(RosterKey);
        PlayerPrefs.Save();
    }

    static List<string> Roster()
    {
        string raw = PlayerPrefs.GetString(RosterKey, "");
        return raw.Split(Separator).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    static string Clean(string value, string fallback)
    {
        value = (value ?? "").Trim();
        return value.Length == 0 ? fallback : value.ToUpperInvariant();
    }
}
