﻿namespace Notan;

public interface IServerSystem<TEntity> where TEntity : struct, IEntity<TEntity>
{
    void Work(ServerHandle<TEntity> handle, ref TEntity entity);
    void PreWork() { }
    void PostWork() { }
}

public interface IClientSystem<TEntity> where TEntity : struct, IEntity<TEntity>
{
    void Work(ClientHandle<TEntity> handle, ref TEntity entity);
    void PreWork() { }
    void PostWork() { }
}
