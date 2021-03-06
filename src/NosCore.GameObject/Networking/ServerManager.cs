﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Logger;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Domain.Map;

namespace NosCore.GameObject.Networking
{
    public class ServerManager : BroadcastableBase
    {
        private static ServerManager instance;

        private ServerManager() { }

        public static ServerManager Instance
        {
            get
            {
                return instance ?? (instance = new ServerManager());
            }
        }


        private static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances = new ConcurrentDictionary<Guid, MapInstance>();

        private static readonly List<MapDTO> _maps = new List<MapDTO>();

        public MapInstance GenerateMapInstance(short mapId, MapInstanceType type)
        {
            MapDTO map = _maps.Find(m => m.MapId.Equals(mapId));
            if (map == null)
            {
                return null;
            }
            Guid guid = Guid.NewGuid();
            MapInstance mapInstance = new MapInstance(map, guid, false, type);
            _mapinstances.TryAdd(guid, mapInstance);
            return mapInstance;
        }

        public void Initialize()
        {
            // parse rates
            try
            {
                int i = 0;
                int monstercount = 0;
                OrderablePartitioner<MapDTO> mapPartitioner = Partitioner.Create(DAOFactory.MapDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
                ConcurrentDictionary<short, MapDTO> _mapList = new ConcurrentDictionary<short, MapDTO>();
                Parallel.ForEach(mapPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, map =>
                {
                    Guid guid = Guid.NewGuid();
                    MapDTO mapinfo = new MapDTO()
                    {

                        Music = map.Music,
                        Data = map.Data,
                        MapId = map.MapId
                    };
                    _mapList[map.MapId] = mapinfo;
                    MapInstance newMap = new MapInstance(mapinfo, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance);
                    _mapinstances.TryAdd(guid, newMap);

                    monstercount += newMap.Monsters.Count;
                    i++;
                });
                _maps.AddRange(_mapList.Select(s => s.Value));
                if (i != 0)
                {
                    Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPS_LOADED), i));
                }
                else
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NO_MAP));
                }
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPMONSTERS_LOADED), monstercount));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("General Error", ex);
            }
        }

        public Guid GetBaseMapInstanceIdByMapId(short MapId)
        {
            return _mapinstances.FirstOrDefault(s => s.Value?.Map.MapId == MapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public MapInstance GetMapInstance(Guid id)
        {
            return _mapinstances.ContainsKey(id) ? _mapinstances[id] : null;
        }

        internal void RegisterSession(ClientSession clientSession)
        {
            Sessions.TryAdd(clientSession.SessionId, clientSession);
        }

        internal void UnregisterSession(ClientSession clientSession)
        {
           Sessions.TryRemove(clientSession.SessionId, out ClientSession clientSessionuseless);
        }
    }
}
