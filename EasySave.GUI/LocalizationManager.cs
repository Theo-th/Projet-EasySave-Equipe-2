using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using EasySave.Core.Properties;

namespace EasySave.GUI;

public static class LocalizationManager
{
    public static event EventHandler? LanguageChanged;

    // Cache pour améliorer les performances
    private static readonly Dictionary<string, string> _stringCache = new Dictionary<string, string>(100);
    private static string _currentCulture = string.Empty;

    public static void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        
        // Invalider le cache quand la langue change
        if (_currentCulture != cultureCode)
        {
            _stringCache.Clear();
            _currentCulture = cultureCode;
        }
        
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string GetString(string key)
    {
        // Vérifier le cache d'abord
        if (_stringCache.TryGetValue(key, out var cachedValue))
            return cachedValue;

        // Si pas en cache, récupérer et mettre en cache
        var value = Lang.ResourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture) ?? key;
        _stringCache[key] = value;
        return value;
    }
}
