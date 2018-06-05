/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
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
using MCGalaxy.Eco;

namespace MCGalaxy.Commands.Eco {
    public sealed class CmdAward : Command {      
        public override string name { get { return "Award"; } }
        public override string type { get { return CommandTypes.Economy; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        
        public override void Use(Player p, string message) {
            bool take = false;
            if (message.CaselessStarts("give ")) {
                message = message.Substring(5);
            } else if (message.CaselessStarts("take ")) {
                message = message.Substring(5); take = true;
            }
                    
            string[] args = message.SplitSpaces(2);
            if (args.Length < 2) { Help(p); return; }           
            string plName = PlayerInfo.FindMatchesPreferOnline(p, args[0]);
            if (plName == null) return;
            string award = Matcher.FindAwards(p, args[1]);
            if (award == null) { Player.Message(p, "Use %T/Awards %Sfor a list of awards"); return; }

            string displayName = PlayerInfo.GetColoredName(p, plName);
            if (!take) {
                if (Awards.GiveAward(plName, award)) {
                    Chat.MessageGlobal("{0} %Swas awarded: &b{1}", displayName, award);
                    Awards.SavePlayers();
                } else if (p != null && plName.CaselessEq(p.name)) {
                    Player.Message(p, "You already have that award.");
                } else {
                    Player.Message(p, "{0} %Salready has that award.", displayName);
                }
            } else {
                if (Awards.TakeAward(plName, award)) {
                    Chat.MessageGlobal("{0} %Shad their &b{1} %Saward removed", displayName, award);
                    Awards.SavePlayers();
                } else if (p != null && plName.CaselessEq(p.name)) {
                    Player.Message(p, "You did not have that award to begin with.");
                } else {
                    Player.Message(p, "{0} %Sdid not have that award to begin with.", displayName);
                }
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Award give/take [player] [award]");
            Player.Message(p, "%HGives/takes [award] award to/from [player]");
            Player.Message(p, "%T/Award [player] [award]");
            Player.Message(p, "%HShorthand for %T/Award give [player] [award]");
        }
    }
}
