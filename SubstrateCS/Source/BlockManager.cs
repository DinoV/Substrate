using System;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate
{
    public class AlphaBlockManager : BlockManager
    {
        public AlphaBlockManager (IChunkManager cm)
            : base(cm)
        {
            IChunk c = AlphaChunk.Create(0, 0);

            chunkXDim = c.Blocks.XDim;
            chunkYDim = c.Blocks.YDim;
            chunkZDim = c.Blocks.ZDim;
            chunkXMask = chunkXDim - 1;
            chunkYMask = chunkYDim - 1;
            chunkZMask = chunkZDim - 1;
            chunkXLog = Log2(chunkXDim);
            chunkYLog = Log2(chunkYDim);
            chunkZLog = Log2(chunkZDim);
        }
    }

    public class AnvilBlockManager : BlockManager
    {
        public AnvilBlockManager (IChunkManager cm)
            : base(cm)
        {
            IChunk c = AnvilChunk.Create(0, 0);

            chunkXDim = c.Blocks.XDim;
            chunkYDim = c.Blocks.YDim;
            chunkZDim = c.Blocks.ZDim;
            chunkXMask = chunkXDim - 1;
            chunkYMask = chunkYDim - 1;
            chunkZMask = chunkZDim - 1;
            chunkXLog = Log2(chunkXDim);
            chunkYLog = Log2(chunkYDim);
            chunkZLog = Log2(chunkZDim);
        }
    }

    /// <summary>
    /// Represents an Alpha-compatible interface for globally managing blocks.
    /// </summary>
    public abstract class BlockManager : IVersion10BlockManager, IBlockManager
    {
        public const int MIN_X = -32000000;
        public const int MAX_X = 32000000;
        public const int MIN_Y = 0;
        public const int MAX_Y = 256;
        public const int MIN_Z = -32000000;
        public const int MAX_Z = 32000000;

        protected int chunkXDim;
        protected int chunkYDim;
        protected int chunkZDim;
        protected int chunkXMask;
        protected int chunkYMask;
        protected int chunkZMask;
        protected int chunkXLog;
        protected int chunkYLog;
        protected int chunkZLog;

        protected IChunkManager chunkMan;

        protected ChunkRef cache;

        private bool _autoLight = true;
        private bool _autoFluid = false;
        private bool _autoTileTick = false;

        /// <summary>
        /// Gets or sets a value indicating whether changes to blocks will trigger automatic lighting updates.
        /// </summary>
        public bool AutoLight
        {
            get { return _autoLight; }
            set { _autoLight = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether changes to blocks will trigger automatic fluid updates.
        /// </summary>
        public bool AutoFluid
        {
            get { return _autoFluid; }
            set { _autoFluid = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether changes to blocks will trigger automatic fluid updates.
        /// </summary>
        public bool AutoTileTick
        {
            get { return _autoTileTick; }
            set { _autoTileTick = value; }
        }

        public int ChunkXLog {
            get {
                return chunkXLog;
            }
        }
        public int ChunkYLog {
            get {
                return chunkYLog;
            }
        }

        public int ChunkZLog {
            get {
                return chunkZLog;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="BlockManager"/> instance on top of the given <see cref="IChunkManager"/>.
        /// </summary>
        /// <param name="cm">An <see cref="IChunkManager"/> instance.</param>
        public BlockManager (IChunkManager cm)
        {
            chunkMan = cm;
        }

        /// <summary>
        /// Returns a new <see cref="AlphaBlock"/> object from global coordinates.
        /// </summary>
        /// <param name="x">Global X-coordinate of block.</param>
        /// <param name="y">Global Y-coordinate of block.</param>
        /// <param name="z">Global Z-coordiante of block.</param>
        /// <returns>A new <see cref="AlphaBlock"/> object representing context-independent data of a single block.</returns>
        /// <remarks>Context-independent data excludes data such as lighting.  <see cref="AlphaBlock"/> object actually contain a copy
        /// of the data they represent, so changes to the <see cref="AlphaBlock"/> will not affect this container, and vice-versa.</remarks>
        public AlphaBlock GetBlock (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return null;
            }

            return cache.Blocks.GetBlock(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <summary>
        /// Returns a new <see cref="AlphaBlockRef"/> object from global coordaintes.
        /// </summary>
        /// <param name="x">Global X-coordinate of block.</param>
        /// <param name="y">Global Y-coordinate of block.</param>
        /// <param name="z">Global Z-coordinate of block.</param>
        /// <returns>A new <see cref="AlphaBlockRef"/> object representing context-dependent data of a single block.</returns>
        /// <remarks>Context-depdendent data includes all data associated with this block.  Since a <see cref="AlphaBlockRef"/> represents
        /// a view of a block within this container, any updates to data in the container will be reflected in the <see cref="AlphaBlockRef"/>,
        /// and vice-versa for updates to the <see cref="AlphaBlockRef"/>.</remarks>
        public AlphaBlockRef GetBlockRef (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return new AlphaBlockRef();
            }

            return cache.Blocks.GetBlockRef(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <summary>
        /// Updates a block with values from a <see cref="AlphaBlock"/> object.
        /// </summary>
        /// <param name="x">Global X-coordinate of a block.</param>
        /// <param name="y">Global Y-coordinate of a block.</param>
        /// <param name="z">Global Z-coordinate of a block.</param>
        /// <param name="block">A <see cref="AlphaBlock"/> object to copy block data from.</param>
        public void SetBlock (int x, int y, int z, AlphaBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <summary>
        /// Gets a reference object to a single chunk given global coordinates to a block within that chunk.
        /// </summary>
        /// <param name="x">Global X-coordinate of a block.</param>
        /// <param name="y">Global Y-coordinate of a block.</param>
        /// <param name="z">Global Z-coordinate of a block.</param>
        /// <returns>A <see cref="ChunkRef"/> to a single chunk containing the given block.</returns>
        public ChunkRef GetChunk (int x, int y, int z)
        {
            x >>= chunkXLog;
            z >>= chunkZLog;
            return chunkMan.GetChunkRef(x, z);
        }

        protected int Log2 (int x)
        {
            int c = 0;
            while (x > 1) {
                x >>= 1;
                c++;
            }
            return c;
        }

        /// <summary>
        /// Called by other block-specific 'get' and 'set' functions to filter
        /// out operations on some blocks.  Override this method in derrived
        /// classes to filter the entire BlockManager.
        /// </summary>
        protected virtual bool Check (int x, int y, int z)
        {
            int minimumY = cache == null ? MIN_Y : cache.MinimumY;
            int maximumY = cache == null ? MAX_Y : minimumY + cache.Blocks.YDim;
            return (x >= MIN_X) && (x < MAX_X) &&
                (y >= minimumY) && (y < maximumY) &&
                (z >= MIN_Z) && (z < MAX_Z);
        }

        private int LocalY (int y)
        {
            return y - cache.MinimumY;
        }

        #region IBlockContainer Members

        IBlock IBlockCollection.GetBlock (int x, int y, int z)
        {
            return GetBlock(x, y, z);
        }

        IBlock IBlockCollection.GetBlockRef (int x, int y, int z)
        {
            return GetBlockRef(x, y, z);
        }

        /// <inheritdoc/>
        public void SetBlock (int x, int y, int z, IBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <inheritdoc/>
        public BlockInfo GetInfo (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return null;
            }

            return cache.Blocks.GetInfo(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public int GetID (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null) {
                return 0;
            }

            return cache.Blocks.GetID(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetID (int x, int y, int z, int id)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            bool autolight = cache.Blocks.AutoLight;
            bool autofluid = cache.Blocks.AutoFluid;
            bool autoTileTick = cache.Blocks.AutoTileTick;

            cache.Blocks.AutoLight = _autoLight;
            cache.Blocks.AutoFluid = _autoFluid;
            cache.Blocks.AutoTileTick = _autoTileTick;

            cache.Blocks.SetID(x & chunkXMask, LocalY(y), z & chunkZMask, id);

            cache.Blocks.AutoFluid = autofluid;
            cache.Blocks.AutoLight = autolight;
            cache.Blocks.AutoTileTick = autoTileTick;

            UpdateDerivedConnections(x, y, z);
        }

        /// <summary>
        /// Sets a legacy numeric block ID and metadata value atomically.
        /// Anvil palette chunks serialize the pair as its modern namespaced
        /// block state; older chunks retain the ID and data values.
        /// </summary>
        public void SetID (int x, int y, int z, int id, int data)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            bool autolight = cache.Blocks.AutoLight;
            bool autofluid = cache.Blocks.AutoFluid;
            bool autoTileTick = cache.Blocks.AutoTileTick;

            cache.Blocks.AutoLight = _autoLight;
            cache.Blocks.AutoFluid = _autoFluid;
            cache.Blocks.AutoTileTick = _autoTileTick;

            cache.Blocks.SetID(
                x & chunkXMask, LocalY(y), z & chunkZMask, id, data);

            cache.Blocks.AutoFluid = autofluid;
            cache.Blocks.AutoLight = autolight;
            cache.Blocks.AutoTileTick = autoTileTick;

            UpdateDerivedConnections(x, y, z);
        }

        /// <inheritdoc/>
        public string GetStringID(int x, int y, int z) {
            cache = GetChunk(x, y, z);
            if (cache == null) {
                return null;
            }

            string modernName = cache.GetBlockName(x & chunkXMask, y, z & chunkZMask);
            if (modernName != null)
                return modernName;
            BlockInfo info = cache.Blocks.GetInfo(x & chunkXMask, LocalY(y), z & chunkZMask);
            return info == null ? null : info.StrID;
        }

        /// <summary>Gets a copy of the modern block-state properties at global coordinates.</summary>
        public TagNodeCompound GetBlockProperties (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z))
                return null;

            return cache.GetBlockProperties(x & chunkXMask, y, z & chunkZMask);
        }

        /// <summary>Gets a modern string block-state property at global coordinates.</summary>
        public string GetBlockProperty (int x, int y, int z, string property)
        {
            TagNodeCompound properties = GetBlockProperties(x, y, z);
            TagNode value;
            TagNodeString stringValue;
            return properties != null
                && properties.TryGetValue(property, out value)
                && (stringValue = value as TagNodeString) != null
                ? stringValue.Data
                : null;
        }

        /// <inheritdoc/>
        public void SetStringID(int x, int y, int z, string id) {
            BlockInfo info;
            if (!BlockInfo.BlockNameTable.TryGetValue(id, out info))
                throw new ArgumentException("Unknown block identifier: " + id, "id");
            SetID(x, y, z, info.ID);
        }

        #endregion


        #region IDataBlockCollection Members

        IDataBlock IDataBlockCollection.GetBlock (int x, int y, int z)
        {
            return GetBlock(x, y, z);
        }

        IDataBlock IDataBlockCollection.GetBlockRef (int x, int y, int z)
        {
            return GetBlockRef(x, y, z);
        }

        /// <inheritdoc/>
        public void SetBlock (int x, int y, int z, IDataBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <inheritdoc/>
        public int GetData (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null) {
                return 0;
            }

            return cache.Blocks.GetData(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetData (int x, int y, int z, int data)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetData(x & chunkXMask, LocalY(y), z & chunkZMask, data);
            UpdateDerivedConnections(x, y, z);
        }

        private void UpdateDerivedConnections (int x, int y, int z)
        {
            UpdateDerivedState(x, y, z);
            UpdateDerivedState(x - 1, y, z);
            UpdateDerivedState(x + 1, y, z);
            UpdateDerivedState(x, y, z - 1);
            UpdateDerivedState(x, y, z + 1);
            UpdateDerivedState(x, y - 1, z);
            UpdateDerivedState(x, y + 1, z);
        }

        private void UpdateDerivedState (int x, int y, int z)
        {
            ChunkRef blockChunk = GetChunk(x, y, z);
            if (blockChunk == null) return;
            int localY = y - blockChunk.MinimumY;
            if (localY < 0 || localY >= blockChunk.Blocks.YDim) return;

            int localX = x & chunkXMask;
            int localZ = z & chunkZMask;
            int id = blockChunk.Blocks.GetID(localX, localY, localZ);
            if (!HasDerivedConnections(id)) return;

            int data = blockChunk.Blocks.GetData(localX, localY, localZ);
            string name;
            TagNodeCompound properties;
            if (!BlockInfo.TryGetLegacyBlockState(id, data, out name, out properties)) return;
            if (properties == null) properties = new TagNodeCompound();

            if (IsPaneOrBars(id)) {
                SetBooleanConnection(properties, "north", PaneConnectsTo(x, y, z - 1));
                SetBooleanConnection(properties, "east", PaneConnectsTo(x + 1, y, z));
                SetBooleanConnection(properties, "south", PaneConnectsTo(x, y, z + 1));
                SetBooleanConnection(properties, "west", PaneConnectsTo(x - 1, y, z));
            }
            else if (IsFence(id)) {
                SetBooleanConnection(properties, "north", FenceConnectsTo(x, y, z - 1));
                SetBooleanConnection(properties, "east", FenceConnectsTo(x + 1, y, z));
                SetBooleanConnection(properties, "south", FenceConnectsTo(x, y, z + 1));
                SetBooleanConnection(properties, "west", FenceConnectsTo(x - 1, y, z));
            }
            else if (id == BlockType.COBBLESTONE_WALL) {
                bool north = WallConnectsTo(x, y, z - 1);
                bool east = WallConnectsTo(x + 1, y, z);
                bool south = WallConnectsTo(x, y, z + 1);
                bool west = WallConnectsTo(x - 1, y, z);
                SetWallConnection(properties, "north", north);
                SetWallConnection(properties, "east", east);
                SetWallConnection(properties, "south", south);
                SetWallConnection(properties, "west", west);
                bool straight = (north && south && !east && !west) || (east && west && !north && !south);
                SetBooleanConnection(properties, "up", !straight || GetBlockIDAt(x, y + 1, z) != BlockType.AIR);
            }
            else if (id == BlockType.REDSTONE_WIRE) {
                SetWireConnection(properties, "north", x, y, z - 1);
                SetWireConnection(properties, "east", x + 1, y, z);
                SetWireConnection(properties, "south", x, y, z + 1);
                SetWireConnection(properties, "west", x - 1, y, z);
            }
            else if (id == BlockType.TRIPWIRE) {
                SetBooleanConnection(properties, "north", IsTripwireConnector(GetBlockIDAt(x, y, z - 1)));
                SetBooleanConnection(properties, "east", IsTripwireConnector(GetBlockIDAt(x + 1, y, z)));
                SetBooleanConnection(properties, "south", IsTripwireConnector(GetBlockIDAt(x, y, z + 1)));
                SetBooleanConnection(properties, "west", IsTripwireConnector(GetBlockIDAt(x - 1, y, z)));
            }
            else if (id == 199) {
                SetBooleanConnection(properties, "north", IsChorusConnector(GetBlockIDAt(x, y, z - 1), false));
                SetBooleanConnection(properties, "east", IsChorusConnector(GetBlockIDAt(x + 1, y, z), false));
                SetBooleanConnection(properties, "south", IsChorusConnector(GetBlockIDAt(x, y, z + 1), false));
                SetBooleanConnection(properties, "west", IsChorusConnector(GetBlockIDAt(x - 1, y, z), false));
                SetBooleanConnection(properties, "up", IsChorusConnector(GetBlockIDAt(x, y + 1, z), false));
                SetBooleanConnection(properties, "down", IsChorusConnector(GetBlockIDAt(x, y - 1, z), true));
            }
            blockChunk.SetBlockState(localX, y, localZ, name, properties);
        }

        private bool PaneConnectsTo (int x, int y, int z)
        {
            ChunkRef neighbor = GetChunk(x, y, z);
            if (neighbor == null) return false;
            int localY = y - neighbor.MinimumY;
            if (localY < 0 || localY >= neighbor.Blocks.YDim) return false;
            int id = neighbor.Blocks.GetID(x & chunkXMask, localY, z & chunkZMask);
            BlockInfo info = BlockInfo.BlockTable[id];
            return IsPaneOrBars(id)
                || (info != null && info.State == BlockState.SOLID);
        }

        private bool FenceConnectsTo (int x, int y, int z)
        {
            int id = GetBlockIDAt(x, y, z);
            BlockInfo info = BlockInfo.BlockTable[id];
            return IsFence(id) || IsFenceGate(id)
                || (info != null && info.State == BlockState.SOLID);
        }

        private bool WallConnectsTo (int x, int y, int z)
        {
            int id = GetBlockIDAt(x, y, z);
            BlockInfo info = BlockInfo.BlockTable[id];
            return id == BlockType.COBBLESTONE_WALL || IsFenceGate(id)
                || (info != null && info.State == BlockState.SOLID);
        }

        private void SetWireConnection (TagNodeCompound properties, string direction, int x, int y, int z)
        {
            int neighbor = GetBlockIDAt(x, y, z);
            string value = IsRedstoneConnector(neighbor) ? "side" : "none";
            BlockInfo info = BlockInfo.BlockTable[neighbor];
            if (info != null && info.State == BlockState.SOLID
                    && GetBlockIDAt(x, y + 1, z) == BlockType.REDSTONE_WIRE)
                value = "up";
            properties[direction] = new TagNodeString(value);
        }

        private int GetBlockIDAt (int x, int y, int z)
        {
            ChunkRef blockChunk = GetChunk(x, y, z);
            if (blockChunk == null) return BlockType.AIR;
            int localY = y - blockChunk.MinimumY;
            if (localY < 0 || localY >= blockChunk.Blocks.YDim) return BlockType.AIR;
            return blockChunk.Blocks.GetID(x & chunkXMask, localY, z & chunkZMask);
        }

        private static void SetBooleanConnection (TagNodeCompound properties, string name, bool connected)
        {
            properties[name] = new TagNodeString(connected ? "true" : "false");
        }

        private static void SetWallConnection (TagNodeCompound properties, string name, bool connected)
        {
            properties[name] = new TagNodeString(connected ? "low" : "none");
        }

        private static bool HasDerivedConnections (int id)
        {
            return IsPaneOrBars(id) || IsFence(id) || id == BlockType.COBBLESTONE_WALL
                || id == BlockType.REDSTONE_WIRE || id == BlockType.TRIPWIRE || id == 199;
        }

        private static bool IsPaneOrBars (int id)
        {
            return id == BlockType.GLASS_PANE || id == BlockType.STAINED_GLASS_PANE
                || id == BlockType.IRON_BARS;
        }

        private static bool IsFence (int id)
        {
            return id == BlockType.FENCE || id == BlockType.NETHER_BRICK_FENCE
                || (id >= 188 && id <= 192);
        }

        private static bool IsFenceGate (int id)
        {
            return id == BlockType.FENCE_GATE || (id >= 183 && id <= 187);
        }

        private static bool IsRedstoneConnector (int id)
        {
            return id == BlockType.REDSTONE_WIRE || id == 69 || id == 75 || id == 76
                || id == 93 || id == 94 || id == 123 || id == 124
                || id == 149 || id == 150 || id == 151 || id == 178;
        }

        private static bool IsTripwireConnector (int id)
        {
            return id == BlockType.TRIPWIRE || id == BlockType.TRIPWIRE_HOOK;
        }

        private static bool IsChorusConnector (int id, bool allowEndStone)
        {
            return id == 199 || id == 200 || (allowEndStone && id == BlockType.END_STONE);
        }

        #endregion


        #region ILitBlockContainer Members

        ILitBlock ILitBlockCollection.GetBlock (int x, int y, int z)
        {
            throw new NotImplementedException();
        }

        ILitBlock ILitBlockCollection.GetBlockRef (int x, int y, int z)
        {
            return GetBlockRef(x, y, z);
        }

        /// <inheritdoc/>
        public void SetBlock (int x, int y, int z, ILitBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <inheritdoc/>
        public int GetBlockLight (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null) {
                return 0;
            }

            return cache.Blocks.GetBlockLight(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public int GetSkyLight (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null) {
                return 0;
            }

            return cache.Blocks.GetSkyLight(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetBlockLight (int x, int y, int z, int light)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlockLight(x & chunkXMask, LocalY(y), z & chunkZMask, light);
        }

        /// <inheritdoc/>
        public void SetSkyLight (int x, int y, int z, int light)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetSkyLight(x & chunkXMask, LocalY(y), z & chunkZMask, light);
        }

        /// <inheritdoc/>
        public int GetHeight (int x, int z)
        {
            cache = GetChunk(x, 0, z);
            if (cache == null || !Check(x, 0, z)) {
                return 0;
            }

            return cache.Blocks.GetHeight(x & chunkXMask, z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetHeight (int x, int z, int height)
        {
            cache = GetChunk(x, 0, z);
            if (cache == null || !Check(x, 0, z)) {
                return;
            }

            cache.Blocks.SetHeight(x & chunkXMask, z & chunkZMask, height);
        }

        /// <inheritdoc/>
        public void UpdateBlockLight (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.UpdateBlockLight(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void UpdateSkyLight (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.UpdateBlockLight(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        #endregion


        #region IPropertyBlockContainer Members

        IPropertyBlock IPropertyBlockCollection.GetBlock (int x, int y, int z)
        {
            return GetBlock(x, y, z);
        }

        IPropertyBlock IPropertyBlockCollection.GetBlockRef (int x, int y, int z)
        {
            return GetBlockRef(x, y, z);
        }

        /// <inheritdoc/>
        public void SetBlock (int x, int y, int z, IPropertyBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <inheritdoc/>
        public TileEntity GetTileEntity (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return null;
            }

            return cache.Blocks.GetTileEntity(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetTileEntity (int x, int y, int z, TileEntity te)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetTileEntity(x & chunkXMask, LocalY(y), z & chunkZMask, te);
        }

        /// <inheritdoc/>
        public void CreateTileEntity (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.CreateTileEntity(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void ClearTileEntity (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.ClearTileEntity(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        #endregion


        #region IActiveBlockContainer Members

        IActiveBlock IActiveBlockCollection.GetBlock (int x, int y, int z)
        {
            return GetBlock(x, y, z);
        }

        IActiveBlock IActiveBlockCollection.GetBlockRef (int x, int y, int z)
        {
            return GetBlockRef(x, y, z);
        }

        /// <inheritdoc/>
        public void SetBlock (int x, int y, int z, IActiveBlock block)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetBlock(x & chunkXMask, LocalY(y), z & chunkZMask, block);
        }

        /// <inheritdoc/>
        public int GetTileTickValue (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return 0;
            }

            return cache.Blocks.GetTileTickValue(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetTileTickValue (int x, int y, int z, int tickValue)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetTileTickValue(x & chunkXMask, LocalY(y), z & chunkZMask, tickValue);
        }

        /// <inheritdoc/>
        public TileTick GetTileTick (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return null;
            }

            return cache.Blocks.GetTileTick(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void SetTileTick (int x, int y, int z, TileTick te)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.SetTileTick(x & chunkXMask, LocalY(y), z & chunkZMask, te);
        }

        /// <inheritdoc/>
        public void CreateTileTick (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.CreateTileTick(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        /// <inheritdoc/>
        public void ClearTileTick (int x, int y, int z)
        {
            cache = GetChunk(x, y, z);
            if (cache == null || !Check(x, y, z)) {
                return;
            }

            cache.Blocks.ClearTileTick(x & chunkXMask, LocalY(y), z & chunkZMask);
        }

        #endregion
    }
}
