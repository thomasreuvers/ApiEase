// ReSharper disable All
using System.Diagnostics.CodeAnalysis;

namespace ApiEase.Core.Contracts;

public interface IBaseClient;

public interface IBaseClient<TSettings> : IBaseClient 
    where TSettings : IBaseSettings;
    
public interface IBaseClient<TSettings, TDelegatingHandler> : IBaseClient<TSettings>
    where TSettings : IBaseSettings
    where TDelegatingHandler : DelegatingHandler;