using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Provides the bundled Unity String Tables to the standard Localization API.</summary>
[Serializable]
[UnityEngine.Scripting.Preserve]
public sealed class ResourceStringTableProvider : ITableProvider
{
    public AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName, Locale locale)
        where TTable : LocalizationTable
    {
        if (typeof(TTable) != typeof(StringTable) || tableCollectionName != GameLocalization.TableName)
            return default;

        TTable table = Resources.Load<TTable>(GameLocalization.TablePath(locale.Identifier.Code));
        return Addressables.ResourceManager.CreateCompletedOperation(table,
            table == null ? $"Missing bundled localization table: {locale.Identifier.Code}" : null);
    }
}
