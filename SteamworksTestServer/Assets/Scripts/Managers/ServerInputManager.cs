using System.Collections;
using System.Collections.Generic;
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
        /// TODO: Removes input snapshots from old ticks
        /// </summary>
        public void CleanBuffer()
        {
            
        }
    }

    public static ServerInputManager Instance;

    public List<PlayerInputBuffer> playerInputBuffers;

    public uint gameTick => ServerWorldManager.Instance.gameTick;

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
        }
    }

    public void RunNextInputFrame()
    {
        for (int i = 0; i < playerInputBuffers.Count; i++)
        {
            PlayerInputSnapshot curSnapshot = playerInputBuffers[i].buffer.GetValueOrDefault(gameTick, new PlayerInputSnapshot());

            ServerWorldManager.Instance.playerManagers[i].RunServerFixedUpdate(curSnapshot);

            playerInputBuffers[i].buffer.Remove(gameTick);
        }
    }
}
