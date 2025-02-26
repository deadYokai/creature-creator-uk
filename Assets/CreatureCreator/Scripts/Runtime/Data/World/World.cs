// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

namespace DanielLochner.Assets.CreatureCreator
{
    public class World
    {
        #region Properties
        public bool CreativeMode { get; protected set; }
        public bool EnablePVE { get; protected set; }
        public bool SpawnNPC { get; protected set; }
        public string MapName { get; protected set; }
        public string MapId { get; protected set; }
        public int SpawnPoint { get; protected set; }
        #endregion
    }
}