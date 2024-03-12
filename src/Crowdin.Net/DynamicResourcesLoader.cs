using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;
using Crowdin.Net.Infrastructure;
using Crowdin.Net.Models;
using JetBrains.Annotations;

#nullable enable

namespace Crowdin.Net
{
    [PublicAPI]
    public static class DynamicResourcesLoader
    {
        private static CultureInfo? _mCustomCulture;
        private static readonly CultureInfo DefaultCulture = CultureInfo.CurrentUICulture;
        
        public static CultureInfo CurrentCulture
        {
            get => _mCustomCulture ?? DefaultCulture;
            set => _mCustomCulture = value;
        }
        
        public static CrowdinOptions GlobalOptions { get; set; } = new();
        
        public static void LoadStaticStrings(ResourceManager resourceManager, IDictionary dictionary)
        {
            ResourceSet? resourceSet = resourceManager.GetResourceSet(CurrentCulture, true, true);
            
            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry is {Key: string keyString, Value: string valueString})
                {
                    dictionary[keyString] = valueString;
                }
            }
        }
        
        public static Task LoadCrowdinStrings(string filename, IDictionary destinationResources, bool replaceExistingKeys = true)
        {
            var options = (CrowdinOptions) GlobalOptions.Clone();
            options.FileName = filename;
            return LoadCrowdinStrings(options, destinationResources, replaceExistingKeys);
        }
        
        /* Flow
         * 
         * 1) Cache disabled, network denied -> return
         * 
         * 2) Cache disabled, network allowed -> get directly from Crowdin -> update resources
         *
         * 3) Cache enabled, not saved yet, network denied -> return
         *
         * 4) Cache enabled, not saved yet, network allowed -> get from Crowdin
         *    Secondary thread: save to cache
         * 
         * 5) Cache enabled, cache found -> use cache
         *    Secondary thread:
         *    1) Check network denied -> return
         *    2) Update manifest
         *    3) Check Cache.UpdatedAt == Manifest.Timestamp -> return
         *    4) Update resources
         */
        public static async Task LoadCrowdinStrings(CrowdinOptions options, IDictionary destinationResources, bool replaceExistingKeys)
        {
            try
            {
                IDictionary<string, string>? translations;
                bool useNetwork = IsNetworkAllowed(options.NetworkPolicy);
                
                #region 1 - Cache disabled, network denied
                
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (!options.UseCache && !useNetwork) return;
                
                #endregion
                
                #region 2 - Cache disabled, network allowed
                
                if (!options.UseCache)
                {
                    await СrowdinClient.Init(options.DistributionHash);
                    
                    translations = await СrowdinClient.GetFileTranslations(options.FileName);
                    
                    CopyResources(translations, destinationResources, replaceExistingKeys);
                    return;
                }
                
                #endregion
                
                translations = ResourcesCacheManager.GetCachedCopy(options.FileName);
                
                #region 3 - Cache enabled, not saved yet, network denied
                
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (translations is null && !useNetwork) return;
                
                #endregion
                
                #region 4 - Cache enabled, not saved yet, network allowed
                
                if (translations is null)
                {
                    await СrowdinClient.Init(options.DistributionHash);
                    
                    translations = await СrowdinClient.GetFileTranslations(options.FileName);
                    
                    CopyResources(translations, destinationResources, replaceExistingKeys);
                    ResourcesCacheManager.SaveToCache(options.FileName, translations);
                    
                    return;
                }
                
                #endregion
                
                #region 5 - Cache enabled, file found
                
                CopyResources(translations, destinationResources, replaceExistingKeys);
                
                #region 5.1 - Network denied -> no check after cache copy
                
                if (!useNetwork) return;

                #endregion

                #region 5.2 - Network allowed -> update cache asynchronously

                Task.Run(async () => {
                    await СrowdinClient.Init(options.DistributionHash);
                    if (!СrowdinClient.IsInitialized) return;
                    
                    DateTimeOffset remoteUpdateTime = СrowdinClient.Manifest!.Timestamp;
                    bool isCacheUpToDate = ResourcesCacheManager.IsCacheUpToDate(options.FileName, remoteUpdateTime);
                    
                    if (!isCacheUpToDate)
                    {
                        IDictionary<string, string> newResources = await СrowdinClient.GetFileTranslations(options.FileName);
                        
                        CopyResources(newResources, destinationResources, replaceExistingKeys);
                        
                        ResourcesCacheManager.SaveToCache(options.FileName, newResources);
                    }
                });
                
                #endregion
                
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Crowdin DynamicResourcesLoader: {0}", ex.Message);
            }
        }
        
        private static void CopyResources(IDictionary<string, string> source, IDictionary destination, bool replaceExistingKeys)
        {
            foreach (KeyValuePair<string,string> kvp in source)
            {
                if (replaceExistingKeys && destination.Contains(kvp.Key))
                    destination[kvp.Key] = kvp.Value;
                else
                    destination.Add(kvp.Key, kvp.Value);
            }
        }
        
        private static bool IsNetworkAllowed(NetworkPolicy policy)
        {
            return policy switch
            {
                NetworkPolicy.ALL => true,
                NetworkPolicy.FORBIDDEN => false,

                //NetworkPolicy.OnlyMetered =>
                //    Connectivity.ConnectionProfiles
                //        .Any(profile => profile is ConnectionProfile.WiFi),
                
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
            };
        }
    }
}