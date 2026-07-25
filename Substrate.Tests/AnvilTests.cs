using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.Tests
{
    [TestClass]
    public class AnvilTests
    {
        [TestMethod]
        public void PaletteSectionReadsAndWritesPre116PackedStates()
        {
            AssertSectionRoundTrip(2230, false);
        }

        [TestMethod]
        public void PaletteSectionReadsAndWrites116PaddedStates()
        {
            AssertSectionRoundTrip(2586, true);
        }

        [TestMethod]
        public void AquaticChunkSavePreservesDataVersionAndBlockChanges()
        {
            AquaticChunk chunk = AquaticChunk.Create(3, -2);
            chunk.Blocks.SetID(4, 65, 7, BlockType.STONE);

            MemoryStream stream = new MemoryStream();
            Assert.IsTrue(chunk.Save(stream));
            stream.Position = 0;

            NbtTree saved = new NbtTree(stream);
            Assert.AreEqual(1631, saved.Root["DataVersion"].ToTagInt().Data);
            AquaticChunk reloaded = AquaticChunk.CreateVerified(saved);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(3, reloaded.X);
            Assert.AreEqual(-2, reloaded.Z);
            Assert.AreEqual(BlockType.STONE, reloaded.Blocks.GetID(4, 65, 7));
        }

        [TestMethod]
        public void PalettePropertiesSurviveAnUnmodifiedRoundTrip()
        {
            TagNodeList palette = new TagNodeList(TagType.TAG_COMPOUND);
            palette.Add(PaletteEntry("minecraft:air"));
            TagNodeCompound north = PaletteEntry("minecraft:oak_stairs");
            north["Properties"] = Properties("facing", "north");
            TagNodeCompound south = PaletteEntry("minecraft:oak_stairs");
            south["Properties"] = Properties("facing", "south");
            palette.Add(north);
            palette.Add(south);

            int[] states = new int[4096];
            states[0] = 1;
            states[1] = 2;
            TagNodeCompound tree = BuildSection(states, false);
            tree["Palette"] = palette;

            TagNodeList rebuilt = new AquaticSection(tree, 2230).BuildTree().ToTagCompound()["Palette"].ToTagList();
            Assert.AreEqual(3, rebuilt.Count);
            Assert.AreEqual("north", rebuilt[0].ToTagCompound()["Properties"].ToTagCompound()["facing"].ToTagString().Data);
            Assert.AreEqual("south", rebuilt[1].ToTagCompound()["Properties"].ToTagCompound()["facing"].ToTagString().Data);
        }

        [TestMethod]
        public void ModernChunkReadsNegativeSectionsAndPreservesModernTags()
        {
            int[] states = new int[4096];
            states[0] = 1;
            TagNodeCompound section = BuildModernSection(-4, states);
            TagNodeList sections = new TagNodeList(TagType.TAG_COMPOUND);
            sections.Add(section);

            TagNodeCompound root = new TagNodeCompound();
            root["DataVersion"] = new TagNodeInt(5000);
            root["xPos"] = new TagNodeInt(8);
            root["zPos"] = new TagNodeInt(-9);
            root["Status"] = new TagNodeString("full");
            root["sections"] = sections;
            root["block_entities"] = new TagNodeList(TagType.TAG_COMPOUND);

            AquaticChunk chunk = AquaticChunk.CreateVerified(new NbtTree(root));
            Assert.IsNotNull(chunk);
            Assert.AreEqual(-64, chunk.MinimumY);
            Assert.AreEqual(384, chunk.Blocks.YDim);
            Assert.AreEqual(BlockType.STONE, chunk.Blocks.GetID(0, 0, 0));
            Assert.AreEqual(BlockType.STONE, chunk.GetBlockID(0, -64, 0));
            Assert.IsTrue(chunk.IsTerrainPopulated);
            TagNodeCompound properties = Properties("variant", "potent");
            chunk.SetBlockState(1, 319, 2, "minecraft:potent_sulfur", properties);

            MemoryStream stream = new MemoryStream();
            Assert.IsTrue(chunk.Save(stream));
            stream.Position = 0;
            NbtTree saved = new NbtTree(stream);
            Assert.IsFalse(saved.Root.ContainsKey("Level"));
            Assert.AreEqual(5000, saved.Root["DataVersion"].ToTagInt().Data);
            Assert.IsTrue(saved.Root.ContainsKey("sections"));
            Assert.IsFalse(saved.Root.ContainsKey("Biomes"));

            TagNodeCompound savedSection = saved.Root["sections"].ToTagList()[0].ToTagCompound();
            Assert.IsTrue(savedSection.ContainsKey("block_states"));
            Assert.IsTrue(savedSection.ContainsKey("biomes"));

            AquaticChunk reloaded = AquaticChunk.CreateVerified(saved);
            Assert.AreEqual("minecraft:potent_sulfur", reloaded.GetBlockName(1, 319, 2));
        }

        [TestMethod]
        public void RegionFileRoundTripsOversizedExternalChunkStreams()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Substrate-Anvil-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try {
                string regionPath = Path.Combine(directory, "r.-2.3.mca");
                byte[] data = new byte[1100 * 1024];
                new Random(12345).NextBytes(data);

                using (RegionFile region = new RegionFile(regionPath)) {
                    using (Stream output = region.GetChunkDataOutputStream(2, 4))
                        output.Write(data, 0, data.Length);

                    Assert.IsTrue(File.Exists(Path.Combine(directory, "c.-62.100.mcc")));
                    using (Stream input = region.GetChunkDataInputStream(2, 4)) {
                        MemoryStream copy = new MemoryStream();
                        input.CopyTo(copy);
                        CollectionAssert.AreEqual(data, copy.ToArray());
                    }
                }
            } finally {
                Directory.Delete(directory, true);
            }
        }

        private static void AssertSectionRoundTrip(int dataVersion, bool padded)
        {
            int[] states = new int[4096];
            for (int i = 0; i < states.Length; i++) states[i] = (i % 17 == 0) ? 1 : 0;

            TagNodeCompound sectionTree = BuildSection(states, padded);
            AquaticSection section = new AquaticSection(sectionTree, dataVersion);
            Assert.AreEqual(BlockType.STONE, section.Blocks[0, 0, 0]);
            Assert.AreEqual(BlockType.AIR, section.Blocks[1, 0, 0]);
            Assert.AreEqual(BlockType.STONE, section.Blocks[1, 0, 1]);

            section.Blocks[2, 3, 4] = BlockType.STONE;
            TagNodeCompound rebuilt = section.BuildTree().ToTagCompound();
            AquaticSection roundTrip = new AquaticSection(rebuilt, dataVersion);
            Assert.AreEqual(BlockType.STONE, roundTrip.Blocks[2, 3, 4]);
            Assert.AreEqual(BlockType.STONE, roundTrip.Blocks[0, 0, 0]);
        }

        private static TagNodeCompound BuildSection(int[] states, bool padded)
        {
            TagNodeList palette = new TagNodeList(TagType.TAG_COMPOUND);
            palette.Add(PaletteEntry("minecraft:air"));
            palette.Add(PaletteEntry("minecraft:stone"));

            TagNodeCompound section = new TagNodeCompound();
            section["Y"] = new TagNodeByte(0);
            section["Palette"] = palette;
            section["BlockStates"] = new TagNodeLongArray(Pack(states, 4, padded));
            section["SkyLight"] = new TagNodeByteArray(new byte[2048]);
            section["BlockLight"] = new TagNodeByteArray(new byte[2048]);
            return section;
        }

        private static TagNodeCompound BuildModernSection(int y, int[] states)
        {
            TagNodeList palette = new TagNodeList(TagType.TAG_COMPOUND);
            palette.Add(PaletteEntry("minecraft:air"));
            palette.Add(PaletteEntry("minecraft:stone"));
            TagNodeCompound blockStates = new TagNodeCompound();
            blockStates["palette"] = palette;
            blockStates["data"] = new TagNodeLongArray(Pack(states, 4, true));

            TagNodeList biomePalette = new TagNodeList(TagType.TAG_STRING);
            biomePalette.Add(new TagNodeString("minecraft:plains"));
            TagNodeCompound biomes = new TagNodeCompound();
            biomes["palette"] = biomePalette;

            TagNodeCompound section = new TagNodeCompound();
            section["Y"] = new TagNodeByte(unchecked((byte)(sbyte)y));
            section["block_states"] = blockStates;
            section["biomes"] = biomes;
            return section;
        }

        private static TagNodeCompound PaletteEntry(string name)
        {
            TagNodeCompound entry = new TagNodeCompound();
            entry["Name"] = new TagNodeString(name);
            return entry;
        }

        private static TagNodeCompound Properties(string name, string value)
        {
            TagNodeCompound properties = new TagNodeCompound();
            properties[name] = new TagNodeString(value);
            return properties;
        }

        private static long[] Pack(int[] values, int bits, bool padded)
        {
            int perLong = 64 / bits;
            int length = padded ? (values.Length + perLong - 1) / perLong : (values.Length * bits + 63) / 64;
            long[] result = new long[length];
            for (int i = 0; i < values.Length; i++) {
                if (padded) {
                    int word = i / perLong;
                    result[word] |= (long)((ulong)values[i] << ((i % perLong) * bits));
                } else {
                    int bit = i * bits;
                    int word = bit / 64;
                    int offset = bit % 64;
                    result[word] |= (long)((ulong)values[i] << offset);
                    if (offset + bits > 64) result[word + 1] |= (long)((ulong)values[i] >> (64 - offset));
                }
            }
            return result;
        }
    }
}
