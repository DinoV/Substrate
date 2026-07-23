using System;
using System.Collections.Generic;
using System.Text;
using Substrate.Nbt;
using Substrate.Core;

namespace Substrate
{
    public class AquaticSection : INbtObject<AquaticSection>, ICopyable<AquaticSection>
    {
        public static SchemaNodeCompound SectionSchema = new SchemaNodeCompound() {
            new SchemaNodeCompound("block_states") {
                new SchemaNodeList("palette", TagType.TAG_COMPOUND, new SchemaNodeCompound() {
                    new SchemaNodeString("Name", null),
                    new SchemaNodeCompound("Properties", SchemaOptions.OPTIONAL)
                }),
                new SchemaNodeLongArray("data", 0, SchemaOptions.OPTIONAL),
            },
            new SchemaNodeCompound("biomes") {
                new SchemaNodeList("palette", TagType.TAG_COMPOUND, new SchemaNodeCompound() {
                    new SchemaNodeString("Name", null),
                }),
                new SchemaNodeLongArray("data", 256, SchemaOptions.OPTIONAL),
            },
            new SchemaNodeArray("SkyLight", 2048),
            new SchemaNodeArray("BlockLight", 2048),
            new SchemaNodeScaler("Y", TagType.TAG_BYTE),
        };

        private const int XDIM = 16;
        private const int YDIM = 16;
        private const int ZDIM = 16;

        private const int MIN_Y = 0;
        private const int MAX_Y = 15;

        private TagNodeCompound _tree;

        private byte _y;
        private YZXShortDataArray _blocks;
        private YZXNibbleArray _data;
        private YZXNibbleArray _blockLight;
        private YZXNibbleArray _skyLight;
        private YZXNibbleArray _addBlocks;
        private PaletteBlock[] _palette;

        private AquaticSection()
        {
        }

        public AquaticSection(int y)
        {
            if (y < MIN_Y || y > MAX_Y)
                throw new ArgumentOutOfRangeException();

            _y = (byte)y;
            BuildNbtTree();
        }

        public AquaticSection(TagNodeCompound tree)
        {
            LoadTree(tree);
        }

        public int Y
        {
            get { return _y; }
            set
            {
                if (value < MIN_Y || value > MAX_Y)
                    throw new ArgumentOutOfRangeException();

                _y = (byte)value;
                _tree["Y"].ToTagByte().Data = _y;
            }
        }

        public YZXShortDataArray Blocks
        {
            get { return _blocks; }
        }

        public YZXNibbleArray Data
        {
            get { return _data; }
        }

        public YZXNibbleArray BlockLight
        {
            get { return _blockLight; }
        }

        public YZXNibbleArray SkyLight
        {
            get { return _skyLight; }
        }

        public YZXNibbleArray AddBlocks
        {
            get { return _addBlocks; }
        }

        public bool CheckEmpty ()
        {
            return CheckBlocksEmpty() && CheckAddBlocksEmpty();
        }

        private bool CheckBlocksEmpty ()
        {
            for (int i = 0; i < _blocks.Length; i++)
                if (_blocks[i] != 0)
                    return false;
            return true;
        }

        private bool CheckAddBlocksEmpty ()
        {
            if (_addBlocks != null)
                for (int i = 0; i < _addBlocks.Length; i++)
                    if (_addBlocks[i] != 0)
                        return false;
            return true;
        }


        public PaletteBlock[] Palette {
            get {
                return _palette;
            }
        }

        #region INbtObject<AquaticSection> Members

        public AquaticSection LoadTree(TagNode tree) {
            TagNodeCompound ctree = tree as TagNodeCompound;
            if (ctree == null) {
                return null;
            }

            _y = ctree["Y"] as TagNodeByte;

            var block_states = ctree["block_states"] as TagNodeCompound;

            var palette = block_states["palette"] as TagNodeList;
            if (palette == null) {
                return null;
            }
            int palIndex = 0;
            _palette = new PaletteBlock[palette.Count];
            foreach (TagNodeCompound pal in palette) {
                string name = pal["Name"] as TagNodeString;
                /*string properties = pal["Properties"] as TagNodeList;
                if (properties != null) {
                    foreach(var prop in properties) {
                        prop["Name"] 
                    }
                }*/

                BlockInfo blockInfo;
                string[] props = _emptyProps;
                if (BlockInfo.BlockNameTable.TryGetValue(name, out blockInfo)) {
                    _palette[palIndex++] = new PaletteBlock(blockInfo, props);
                }
            }

            var data = ctree["Data"] as TagNodeLongArray;

            _blocks = new YZXShortDataArray(new short[YDIM, ZDIM, XDIM]);
            //_blocks = new YZXByteArray(XDIM, YDIM, ZDIM, ctree["Blocks"] as TagNodeByteArray);
            //_data = new YZXNibbleArray(XDIM, YDIM, ZDIM, ctree["Data"] as TagNodeByteArray);
            _skyLight = new YZXNibbleArray(XDIM, YDIM, ZDIM, ctree["SkyLight"] as TagNodeByteArray);
            _blockLight = new YZXNibbleArray(XDIM, YDIM, ZDIM, ctree["BlockLight"] as TagNodeByteArray);

            if (!ctree.ContainsKey("Add"))
                ctree["Add"] = new TagNodeByteArray(new byte[2048]);
            _addBlocks = new YZXNibbleArray(XDIM, YDIM, ZDIM, ctree["Add"] as TagNodeByteArray);

            _tree = ctree;

            return this;
        }

        static string[] _emptyProps = new string[0];

        public AquaticSection LoadTreeSafe (TagNode tree)
        {
            if (!ValidateTree(tree)) {
                return null;
            }

            return LoadTree(tree);
        }

        public TagNode BuildTree ()
        {
            TagNodeCompound copy = new TagNodeCompound();
            foreach (KeyValuePair<string, TagNode> node in _tree) {
                copy.Add(node.Key, node.Value);
            }

            if (CheckAddBlocksEmpty())
                copy.Remove("Add");

            return copy;
        }

        public bool ValidateTree (TagNode tree)
        {
            NbtVerifier v = new NbtVerifier(tree, SectionSchema);
            return v.Verify();
        }

        #endregion

        #region ICopyable<AquaticSection> Members

        public AquaticSection Copy ()
        {
            return new AquaticSection().LoadTree(_tree.Copy());
        }

        #endregion

        private void BuildNbtTree ()
        {
            int elements3 = XDIM * YDIM * ZDIM;

            TagNodeByteArray blocks = new TagNodeByteArray(new byte[elements3]);
            TagNodeByteArray data = new TagNodeByteArray(new byte[elements3 >> 1]);
            TagNodeByteArray skyLight = new TagNodeByteArray(new byte[elements3 >> 1]);
            TagNodeByteArray blockLight = new TagNodeByteArray(new byte[elements3 >> 1]);
            TagNodeByteArray addBlocks = new TagNodeByteArray(new byte[elements3 >> 1]);

            _blocks = new YZXShortDataArray(new short[YDIM, ZDIM, XDIM]);
            _data = new YZXNibbleArray(XDIM, YDIM, ZDIM, data);
            _skyLight = new YZXNibbleArray(XDIM, YDIM, ZDIM, skyLight);
            _blockLight = new YZXNibbleArray(XDIM, YDIM, ZDIM, blockLight);
            _addBlocks = new YZXNibbleArray(XDIM, YDIM, ZDIM, addBlocks);

            TagNodeCompound tree = new TagNodeCompound();
            tree.Add("Y", new TagNodeByte(_y));
            tree.Add("Blocks", blocks);
            tree.Add("Data", data);
            tree.Add("SkyLight", skyLight);
            tree.Add("BlockLight", blockLight);
            tree.Add("Add", addBlocks);

            _tree = tree;
        }
    }

    public struct PaletteBlock
    {
        public BlockInfo Block;
        public string[] Properties;

        public PaletteBlock(BlockInfo blockInfo, string[] properties) : this() {
            Block = blockInfo;
            Properties = properties;
        }
    }
}
