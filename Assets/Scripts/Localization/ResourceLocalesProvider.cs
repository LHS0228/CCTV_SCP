using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>Ships locales with the player so language selection needs no content download.</summary>
[Serializable]
[UnityEngine.Scripting.Preserve]
public sealed class ResourceLocalesProvider : ILocalesProvider
{
    [SerializeField] private List<Locale> locales = new List<Locale>();
    public List<Locale> Locales => locales;

    public Locale GetLocale(LocaleIdentifier identifier)
    {
        return locales.Find(locale => locale != null && locale.Identifier == identifier);
    }

    public void AddLocale(Locale locale)
    {
        if (locale != null && GetLocale(locale.Identifier) == null)
            locales.Add(locale);
    }

    public bool RemoveLocale(Locale locale) => locales.Remove(locale);
}
