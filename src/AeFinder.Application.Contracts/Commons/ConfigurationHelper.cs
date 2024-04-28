using System;
using Microsoft.Extensions.Configuration;

namespace AeFinder.Commons;

public class ConfigurationHelper
{
    private static bool _isInitialized = false;
    private static IConfiguration _configuration;

    public static void Initialize(IConfiguration configuration)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("ConfigurationHelper has been initialized.");
        }

        _configuration = configuration;
        _isInitialized = true;
    }

    public static T GetValue<T>(string key, T defaultValue)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ConfigurationHelper has not been initialized.");
        }

        return _configuration.GetValue<T>(key, defaultValue);
    }

    /// <summary>
    /// Apollo Configuration Center degrade switch
    /// </summary>
    /// <returns></returns>
    public static bool IsApolloEnabled()
    {
        return GetValue<bool>("IsApolloEnabled", false);
    }
}