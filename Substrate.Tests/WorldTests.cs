using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Substrate;
using Substrate.TileEntities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
namespace Substrate.Tests
{
    [TestClass]
    public class WorldTests
    {
        [TestMethod]
        public void OpenTest_262_missing_heightmaps()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\26_2-missing-heightmaps\");
            var bm = world.GetBlockManager() as AnvilBlockManager;
            var height = bm.GetHeight(2431, 1911);
            Assert.AreEqual(111, height);
        }
        [TestMethod]
        public void LegacyBrickBlockSavesWithModernName()
        {
            string source = Path.GetFullPath(@"..\..\Data\26_2-missing-heightmaps\");
            string copy = Path.Combine(Path.GetTempPath(), "Substrate-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(source, copy);
            try {
                NbtWorld world = NbtWorld.Open(copy);
                AnvilBlockManager blocks = world.GetBlockManager() as AnvilBlockManager;
                blocks.SetID(2431, 111, 1911, BlockInfo.BrickBlock.ID);
                blocks.SetData(2431, 111, 1911, 0);
                world.Save();

                world = NbtWorld.Open(copy);
                blocks = world.GetBlockManager() as AnvilBlockManager;
                Assert.AreEqual(BlockInfo.BrickBlock.ID, blocks.GetID(2431, 111, 1911));
                Assert.AreEqual("minecraft:bricks", blocks.GetStringID(2431, 111, 1911));
                blocks = null;
                world = null;
            }
            finally {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Directory.Delete(copy, true);
            }
        }

        [TestMethod]
        public void GlassPaneConnectionsAreSavedFromNeighboringWalls()
        {
            string source = Path.GetFullPath(@"..\..\Data\26_2-missing-heightmaps\");
            string copy = Path.Combine(Path.GetTempPath(), "Substrate-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(source, copy);
            try {
                NbtWorld world = NbtWorld.Open(copy);
                AnvilBlockManager blocks = world.GetBlockManager() as AnvilBlockManager;
                const int x = 2430;
                const int y = 112;
                const int z = 1911;
                blocks.SetID(x, y, z - 1, BlockType.STONE);
                blocks.SetID(x, y, z + 1, BlockType.STONE);
                blocks.SetID(x - 1, y, z, BlockType.AIR);
                blocks.SetID(x + 1, y, z, BlockType.AIR);
                blocks.SetID(x, y, z, BlockType.GLASS_PANE);
                blocks.SetData(x, y, z, 0);
                world.Save();

                world = NbtWorld.Open(copy);
                blocks = world.GetBlockManager() as AnvilBlockManager;
                Assert.AreEqual("true", blocks.GetBlockProperty(x, y, z, BlockProperties.North));
                Assert.AreEqual("false", blocks.GetBlockProperty(x, y, z, BlockProperties.East));
                Assert.AreEqual("true", blocks.GetBlockProperty(x, y, z, BlockProperties.South));
                Assert.AreEqual("false", blocks.GetBlockProperty(x, y, z, BlockProperties.West));
                blocks = null;
                world = null;
            }
            finally {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Directory.Delete(copy, true);
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            foreach (string directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
        [TestMethod]
        public void OpenTest_262_creative()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\26_2-creative\");
            Assert.IsNotNull(world);
            Assert.AreEqual(80, world.Level.Spawn.X);
            Assert.AreEqual(63, world.Level.Spawn.Y);
            Assert.AreEqual(240, world.Level.Spawn.Z);

            AnvilWorld anvil = world as AnvilWorld;
            var block = anvil.GetBlockManager().GetBlock(79, 62, 249);
            Assert.AreEqual("minecraft:sand", block.Info.StrID);
            Assert.AreSame(BlockInfo.Sand, block.Info);
            Assert.AreEqual(12, block.Info.ID);
            block = anvil.GetBlockManager().GetBlock(79, 63, 249);
            var height = anvil.GetBlockManager().GetHeight(79, 249);
            Assert.AreEqual(63, height);
            Assert.AreEqual(AcquaticBlocks.LeafLitter, block.Info.StrID);
            Assert.AreEqual("2", anvil.GetBlockManager().GetBlockProperty(79, 63, 249, BlockProperties.SegmentAmount));
            Assert.AreEqual("north", anvil.GetBlockManager().GetBlockProperty(79, 63, 249, BlockProperties.Facing));
            anvil.GetBlockManager().SetID(79, 63, 249, BlockType.SIGN_POST);
            Assert.IsInstanceOfType(
                anvil.GetBlockManager().GetTileEntity(79, 63, 249),
                typeof(TileEntitySign));
            Assert.IsNotNull(anvil);
            Assert.IsTrue(anvil.GetRegionManager().GetRegionPath().EndsWith(
                Path.Combine("dimensions", "minecraft", "overworld", "region")));
            Assert.IsTrue(anvil.GetChunkManager().ChunkExists(-22, -11));
        }

        [TestMethod]
        public void OpenTest_1_6_4_survival()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_6_4-survival\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_7_2_survival()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_7_2-survival\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_7_10_survival()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_7_10-survival\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_8_3_survival()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_8_3-survival\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_8_3_debug()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_8_3-debug\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_8_7_debug()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_8_7-debug\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_8_7_survival()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_8_7-survival\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void OpenTest_1_9_2_debug()
        {
            NbtWorld world = NbtWorld.Open(@"..\..\Data\1_9_2-debug\");
            Assert.IsNotNull(world);
        }

        [TestMethod]
        public void AnvilWorldUsesLegacyOverworldRegionLocation()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Substrate-World-" + Guid.NewGuid().ToString("N"));
            try {
                AnvilWorld.Create(directory).Save();

                AnvilWorld world = AnvilWorld.Open(directory);
                string expected = Path.Combine(directory, "region");
                Assert.AreEqual(expected, world.GetRegionManager().GetRegionPath());

                world.GetRegionManager().CreateRegion(0, 0);
                Assert.IsTrue(File.Exists(Path.Combine(expected, "r.0.0.mca")));
            }
            finally {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AnvilWorldUsesNamespacedOverworldRegionLocation()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Substrate-World-" + Guid.NewGuid().ToString("N"));
            try {
                AnvilWorld.Create(directory).Save();

                string legacy = Path.Combine(directory, "region");
                string modern = Path.Combine(directory, "dimensions", "minecraft", "overworld", "region");
                Directory.CreateDirectory(Path.GetDirectoryName(modern));
                Directory.Move(legacy, modern);

                AnvilWorld world = NbtWorld.Open(directory) as AnvilWorld;
                Assert.IsNotNull(world);
                Assert.AreEqual(modern, world.GetRegionManager().GetRegionPath());

                world.GetRegionManager().CreateRegion(0, 0);
                Assert.IsTrue(File.Exists(Path.Combine(modern, "r.0.0.mca")));
                Assert.IsFalse(Directory.Exists(legacy));
            }
            finally {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }
}
