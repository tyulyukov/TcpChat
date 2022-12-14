using Autofac;
using Autofac.Configuration;
using Blazy.Sockets.Contracts;
using Blazy.Sockets.Handlers;
using Microsoft.Extensions.Configuration;

namespace Blazy.Sockets.Network;

public class NetworkBuilder
{
    private readonly List<string> _packetHandlerEvents;
    private readonly ContainerBuilder _builder;
    
    public NetworkBuilder() : this("appsettings.json") { }

    public NetworkBuilder(string configPath)
    {
        _builder = new ();
        _packetHandlerEvents = new List<string>();

        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, true, true)
            .Build();
        
        _builder.RegisterInstance(config).As<IConfiguration>();
        _builder.RegisterModule(new ConfigurationModule(config));
        
        Use<INetworkServer, NetworkServer>();
        Use<ISocketAcceptor, SocketAcceptor>();
        Use<INetworkClient, NetworkClient>();
        Use<IPacketHandlersContainer, PacketHandlersContainer>();
    }

    public void Use<TInterface, TImplementation>() where TInterface : notnull where TImplementation : TInterface
    {
        _builder.RegisterType<TImplementation>().As<TInterface>().SingleInstance();
    }

    public void Use<TImplementation>() where TImplementation : notnull
    {
        _builder.RegisterType<TImplementation>().AsSelf().SingleInstance();
    }

    public void Use<TInstance>(TInstance instance) where TInstance : class
    {
        _builder.RegisterInstance(instance).AsSelf().SingleInstance();
    }
    
    public void Use<TInterface, TInstance>(TInstance instance) where TInstance : class where TInterface : notnull
    {
        _builder.RegisterInstance(instance).As<TInterface>().SingleInstance();
    }

    public void UsePacketHandler<THandler>(string eventName) where THandler : IPacketHandler
    {
        if (_packetHandlerEvents.Contains(eventName))
            throw new ApplicationException($"Event handler with event name {eventName} already exists");

        _builder.RegisterType<THandler>()
            .Named<IPacketHandler>(eventName)
            .SingleInstance();

        _packetHandlerEvents.Add(eventName);
    }

    public void UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
    {
        _builder.RegisterType<TMiddleware>()
            .As<IMiddleware>()
            .SingleInstance();
    }
    
    public void UseConnectionHandler<THandler>() where THandler : PacketHandler<ConnectionDetails>
    {
        _builder.RegisterType<THandler>()
            .As<PacketHandler<ConnectionDetails>>()
            .SingleInstance();
    }

    public void UseDisconnectionHandler<THandler>() where THandler : PacketHandler<DisconnectionDetails>
    {
        _builder.RegisterType<THandler>()
            .As<PacketHandler<DisconnectionDetails>>()
            .SingleInstance();
    }
    
    protected virtual void BeforeBuild()
    {
        _builder.RegisterType<JsonPacketEncoder>()
            .As<IEncoder<Packet>>()
            .SingleInstance()
            .IfNotRegistered(typeof(IEncoder<Packet>));
    }
    
    public IContainer Build()
    {
        BeforeBuild();
        return _builder.Build();
    }
}