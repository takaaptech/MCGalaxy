/*
    Copyright 2011 MCForge
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;

namespace MCGalaxy.Commands.Building {
    public sealed class CmdFill : DrawCmd {
        public override string name { get { return "fill"; } }
        public override string shortcut { get { return "f"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        protected override string PlaceMessage { get { return "Destroy the block you wish to fill."; } }
        public override int MarksCount { get { return 1; } }
        
        protected override DrawMode GetMode(string[] parts) {
            string msg = parts[parts.Length - 1];
            if (msg == "normal") return DrawMode.solid;
            else if (msg == "up") return DrawMode.up;
            else if (msg == "down") return DrawMode.down;
            else if (msg == "layer") return DrawMode.layer;
            else if (msg == "vertical_x") return DrawMode.verticalX;
            else if (msg == "vertical_z") return DrawMode.verticalZ;
            return DrawMode.normal;
        }
        
        protected override DrawOp GetDrawOp(DrawArgs dArg, Vec3S32[] m) {
            return null;
        }
        
        protected override bool DoDraw(Player p, Vec3S32[] marks, object state, byte type, byte extType) {
            DrawArgs cpos = (DrawArgs)state;
            ushort x = (ushort)marks[0].X, y = (ushort)marks[0].Y, z = (ushort)marks[0].Z;
            byte oldType = p.level.GetTile(x, y, z), oldExtType = 0;
            if (oldType == Block.custom_block)
                oldExtType = p.level.GetExtTile(x, y, z);

            cpos.Block = type; cpos.ExtBlock = extType;
            if (!Block.canPlace(p, oldType) && !Block.BuildIn(oldType)) { 
                Formatter.MessageBlock(p, "fill over ", oldType); return false;
            }

            SparseBitSet bits = new SparseBitSet(p.level.Width, p.level.Height, p.level.Length);
            List<int> buffer = new List<int>(), origins = new List<int>();
            FloodFill(p, x, y, z, oldType, oldExtType, cpos.Mode, bits, buffer, origins, 0);

            int totalFill = origins.Count;
            for (int i = 0; i < totalFill; i++) {
                int pos = origins[i];
                p.level.IntToPos(pos, out x, out y, out z);
                FloodFill(p, x, y, z, oldType, oldExtType, cpos.Mode, bits, buffer, origins, 0);
                totalFill = origins.Count;
            }
            
            FillDrawOp op = new FillDrawOp();
            op.Positions = buffer;
            int brushOffset = cpos.Mode == DrawMode.normal ? 0 : 1;
            Brush brush = ParseBrush(p, cpos, brushOffset);
            if (brush == null || !DrawOp.DoDrawOp(op, brush, p, marks)) return false;
            bits.Clear();
            op.Positions = null;
            return true;
        }

        void FloodFill(Player p, ushort x, ushort y, ushort z, byte block, byte extBlock, DrawMode fillType,
                       SparseBitSet bits, List<int> buffer, List<int> origins, int depth) {
            if (bits.Get(x, y, z) || buffer.Count > p.group.maxBlocks) return;
            int index = p.level.PosToInt(x, y, z);
            if (depth > 2000) { origins.Add(index); return; }
            bits.Set(x, y, z, true);
            buffer.Add(index);

            if (fillType != DrawMode.verticalX) { // x
                if (CheckTile(p, (ushort)(x + 1), y, z, block, extBlock))
                    FloodFill(p, (ushort)(x + 1), y, z, block, extBlock, fillType, bits, buffer, origins, depth + 1);
                if (CheckTile(p, (ushort)(x - 1), y, z, block, extBlock))
                    FloodFill(p, (ushort)(x - 1), y, z, block, extBlock, fillType, bits, buffer, origins, depth + 1);
            }

            if (fillType != DrawMode.verticalZ) { // z
                if (CheckTile(p, x, y, (ushort)(z + 1), block, extBlock))
                    FloodFill(p, x, y, (ushort)(z + 1), block, extBlock, fillType, bits, buffer, origins, depth + 1);
                if (CheckTile(p, x, y, (ushort)(z - 1), block, extBlock))
                    FloodFill(p, x, y, (ushort)(z - 1), block, extBlock, fillType, bits, buffer, origins, depth + 1);
            }

            if (!(fillType == DrawMode.down || fillType == DrawMode.layer)) { // y up
                if (CheckTile(p, x, (ushort)(y + 1), z, block, extBlock))
                    FloodFill(p, x, (ushort)(y + 1), z, block, extBlock, fillType, bits, buffer, origins, depth + 1);
            }

            if (!(fillType == DrawMode.up || fillType == DrawMode.layer)) { // y down
                if (CheckTile(p, x, (ushort)(y - 1), z, block, extBlock))
                    FloodFill(p, x, (ushort)(y - 1), z, block, extBlock, fillType, bits, buffer, origins, depth + 1);
            }
        }
        
        bool CheckTile(Player p, ushort x, ushort y, ushort z, byte block, byte extBlock) {
            byte curBlock = p.level.GetTile(x, y, z);

            if (curBlock == block && curBlock == Block.custom_block) {
                byte curExtBlock = p.level.GetExtTile(x, y, z);
                return curExtBlock == extBlock;
            }
            return curBlock == block;
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/fill [brush args] <mode>");
            Player.Message(p, "%HFills the area specified with the output of the current brush.");
            Player.Message(p, "   %HFor help about brushes, type %T/help brush%H.");
            Player.Message(p, "   %HModes: &fnormal/up/down/layer/vertical_x/vertical_z");            
        }
    }
}
