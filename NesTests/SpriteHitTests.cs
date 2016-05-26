using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components;
using EmulatorCore.Components.Debugging;
using EmulatorCore.Components.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NesTests
{
    [TestClass]
    public class SpriteHitTests : EmulatorTestBase
    {
        private void RunSpriteHitTest(string rom)
        {
            IEmulator emulator = this.GetInstance();
            emulator.LoadFile(rom);

            IMemoryBus cpuBus = (IMemoryBus)emulator.Components.First(c => c.Name == "CPU Bus");
            IComponentWithBreakpoints cpuBusBP = (IComponentWithBreakpoints)cpuBus;

            bool testComplete = false;

            // This is kind of a hack - the test ROMs set 0xF8 to the number of the test that failed, or 1
            //  if all tests pass, but it's incremented as the tests run.  To determine when the test run
            //  is over, look for a write to 0x07F1, which happens in the routine that prints the results,
            //  and check 0xF8 at that point.
            IMemoryBreakpoint memBP = (IMemoryBreakpoint)cpuBusBP.CreateBreakpoint("MemoryBreakpoint");
            memBP.TargetAddress = 0x07F1;
            memBP.AccessType = AccessType.Write;
            memBP.BreakpointHit += (sender, e) =>
            {
                testComplete = true;
            };
            memBP.Enabled = true;

            base.RunEmulator(emulator, () => !testComplete);

            byte value = cpuBus.Read(0xF8);
            if (value != 1)
            {
                // Non-zero value other than 1 indicates failure
                Assert.Fail("Test #{0} failed!", value);
            }
        }

        [TestMethod]
        public void SpriteHitBasics()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\01.basics.nes");
        }

        [TestMethod]
        public void SpriteHitAlignment()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\02.alignment.nes");
        }

        [TestMethod]
        public void SpriteHitCorners()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\03.corners.nes");
        }

        [TestMethod]
        public void SpriteHitFlip()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\04.flip.nes");
        }

        [TestMethod]
        public void SpriteHitLeftClip()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\05.left_clip.nes");
        }

        [TestMethod]
        public void SpriteHitRightEdge()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\06.right_edge.nes");
        }

        [TestMethod]
        public void SpriteHitScreenBottom()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\07.screen_bottom.nes");
        }

        [TestMethod]
        public void SpriteHitDoubleHeight()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\08.double_height.nes");
        }

        [TestMethod]
        public void SpriteHitTimingBasics()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\09.timing_basics.nes");
        }

        [TestMethod]
        public void SpriteHitTimingOrder()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\10.timing_order.nes");
        }

        [TestMethod]
        public void SpriteHitEdgeTiming()
        {
            this.RunSpriteHitTest(@"TestRoms\sprite_hit_tests_2005.10.05\11.edge_timing.nes");
        }
    }
}
