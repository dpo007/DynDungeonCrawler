namespace DynDungeonCrawler.Engine.Interfaces
{
    /// <summary>
    /// Defines standard message types for consistent UI styling across different implementations.
    /// </summary>
    public enum UIMessageType
    {
        /// <summary>Standard informational message</summary>
        Normal,

        /// <summary>Error or warning message</summary>
        Error,

        /// <summary>Success or positive outcome message</summary>
        Success,

        /// <summary>Emphasized or important message</summary>
        Emphasis,

        /// <summary>Section title or header</summary>
        Header,

        /// <summary>Combat action message</summary>
        CombatAction,

        /// <summary>Critical hit or significant event in combat</summary>
        CombatCritical,

        /// <summary>Player status or stat information</summary>
        PlayerStatus,

        /// <summary>Enemy status or stat information</summary>
        EnemyStatus,

        /// <summary>Information about items or treasures</summary>
        ItemInfo,

        /// <summary>Help text or instructions</summary>
        Help,

        /// <summary>Room or environment description</summary>
        RoomDescription,

        /// <summary>System or debug message</summary>
        System
    }
}