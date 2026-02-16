using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using EasySave.Core.Properties;

namespace EasySave.GUI;

/// <summary>
/// Provides localization and language management for the EasySave GUI.
/// Handles culture switching and string retrieval with caching.
/// </summary>
public static class LocalizationManager
{
    /// <summary>
    /// Event triggered when the application language changes.
    /// </summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>
    /// Cache for localized strings to improve performance.
    /// </summary>
    private static readonly Dictionary<string, string> _stringCache = new Dictionary<string, string>(100);
    private static string _currentCulture = string.Empty;

    /// <summary>
    /// Sets the application language and updates thread cultures.
    /// Clears the string cache if the culture changes.
    /// </summary>
    /// <param name="cultureCode">Culture code (e.g., "fr-FR", "en-US").</param>
    public static void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        

        if (_currentCulture != cultureCode)
        {
            _stringCache.Clear();
            _currentCulture = cultureCode;
        }
        
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Retrieves a localized string for the given key, using cache for performance.
    /// </summary>
    /// <param name="key">Resource key for the string.</param>
    /// <returns>Localized string or the key if not found.</returns>
    public static string GetString(string key)
    {

        if (_stringCache.TryGetValue(key, out var cachedValue))
            return cachedValue;


        var value = Lang.ResourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture) ?? key;
        _stringCache[key] = value;
        return value;
    }
}
