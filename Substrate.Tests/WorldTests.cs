using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Substrate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Substrate.Tests
{
    [TestClass]
    public class WorldTests
    {
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
