using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerInputManager : MonoBehaviour
{
    public class PlayerInputBuffer
    {
        /// This input buffer will use frame number as the
        /// key and have PlayerInputSnapshot values.
        public Dictionary<uint, PlayerInputSnapshot> buffer;

        public PlayerInputBuffer()
        {
            buffer = new Dictionary<uint, PlayerInputSnapshot>();
        }

        public void EmptyBuffer() => buffer.Clear();

        /// <summary>
        /// Removes stale input snapshots from old ticks
        /// </summary>
        public void CleanBuffer()
        {
            foreach (var key in buffer.Keys.Where(s => s <= Instance.minValidGameTick).ToList())
            {
                buffer.Remove(key);
            }
        }
    }

    public static ServerInputManager Instance;

    public List<PlayerInputBuffer> playerInputBuffers;

    public uint gameTick => ServerWorldManager.Instance.gameTick;

    public uint minValidGameTick => gameTick - 30;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }

    /// <summary>
    /// Creates a new list of input buffers for the players
    /// </summary>
    /// <param name="count"></param>
    public void CreatePlayerBuffers(int count)
    {
        playerInputBuffers = new List<PlayerInputBuffer>();

        for (int i = 0; i < count; i++)
        {
            playerInputBuffers.Add(new PlayerInputBuffer());
        }
    }

    public void CleanInputBuffers()
    {
        foreach (var buf in playerInputBuffers)
            buf.CleanBuffer();
    }

    public void AddInputFrame(int playerIndex, PlayerInputSnapshot input)
    {
        playerInputBuffers[playerIndex].buffer[input.gameTick] = input;
    }

    public void ProcessInputSnapshotBundle(int playerIndex, PlayerInputSnapshotBundle bundle)
    {
        // Patch the current dictionary with this new list of inputs
        for (int i = 0; i < bundle.snapshots.Length; i++)
        {
            playerInputBuffers[playerIndex].buffer[bundle.snapshots[i].gameTick] = bundle.snapshots[i];

            InputLossMonitor.Instance?.AddServerInputSnapshot(bundle.snapshots[i]);
        }
    }

    public void RunNextInputFrame()
    {
        for (int i = 0; i < playerInputBuffers.Count; i++)
        {
            PlayerInputSnapshot curSnapshot = playerInputBuffers[i].buffer.GetValueOrDefault(gameTick, new PlayerInputSnapshot());

            ServerWorldManager.Instance.playerManagers[i].RunServerFixedUpdate(curSnapshot);

            playerInputBuffers[i].buffer.Remove(gameTick);

            Debug.Log("pib len: " + playerInputBuffers[i].buffer.Count);
        }
    }
}
