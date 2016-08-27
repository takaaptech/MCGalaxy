/*
    Copyright 2011 MCForge
        
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
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using MCGalaxy.Eco;
using MCGalaxy.SQL;

namespace MCGalaxy {
    public static class Economy {

        public static bool Enabled;
        
        const string createTable =
            @"CREATE TABLE if not exists Economy (
player      VARCHAR(20),
money       INT UNSIGNED,
total       INT UNSIGNED NOT NULL DEFAULT 0,
purchase    VARCHAR(255) NOT NULL DEFAULT '%cNone',
payment     VARCHAR(255) NOT NULL DEFAULT '%cNone',
salary      VARCHAR(255) NOT NULL DEFAULT '%cNone',
fine        VARCHAR(255) NOT NULL DEFAULT '%cNone',
PRIMARY KEY(player)
            );";

        public struct EcoStats {
            public string playerName, purchase, payment, salary, fine;
            public int money, totalSpent;
            public EcoStats(string name, int mon, int tot, string pur, string pay, string sal, string fin) {
                playerName = name;
                money = mon;
                totalSpent = tot;
                purchase = pur;
                payment = pay;
                salary = sal;
                fine = fin;
            }
        }

        public static void LoadDatabase() {
        retry:
            Database.Execute(createTable); //create database
            DataTable eco = Database.Fill("SELECT * FROM Economy");
            try {
                DataTable players = Database.Fill("SELECT * FROM Players");
                if (players.Rows.Count == eco.Rows.Count) { } //move along, nothing to do here
                else if (eco.Rows.Count == 0) { //if first time, copy content from player to economy
                    Database.Execute("INSERT INTO Economy (player, money) SELECT Players.Name, Players.Money FROM Players");
                } else {
                    //this will only be needed when the server shuts down while it was copying content (or some other error)
                    Database.Execute("DROP TABLE Economy");
                    goto retry;
                }
                players.Dispose(); eco.Dispose();
            } catch { }
        }

        public static void Load() {
            if (!File.Exists("properties/economy.properties")) { 
                Server.s.Log("Economy properties don't exist, creating"); 
                Save(); 
            }
            
            using (StreamReader r = new StreamReader("properties/economy.properties")) {
                string line;
                while ((line = r.ReadLine()) != null) {
                    line = line.ToLower().Trim();
                    try {
                        ParseLine(line);
                    } catch { }
                }
            }
            Save();
        }
        
        static void ParseLine(string line) {
            string[] args = line.Split(':');
            if (args[0].CaselessEq("enabled")) {
                Enabled = args[1].CaselessEq("true");
            } else if (args.Length >= 3) {
                Item item = GetItem(args[0]);
                if (item == null) return;
                
                if (args[1].CaselessEq("enabled"))
                    item.Enabled = args[2].CaselessEq("true");
                else if (args[1].CaselessEq("purchaserank"))
                    item.PurchaseRank = (LevelPermission)int.Parse(args[2]);
                else
                    item.Parse(line, args);
            }
        }

        public static void Save() {
            using (StreamWriter w = new StreamWriter("properties/economy.properties", false)) {
                w.WriteLine("enabled:" + Enabled);              
                foreach (Item item in Items) {
                    w.WriteLine();
                    item.Serialise(w);                    
                }
            }
        }

        public static EcoStats RetrieveEcoStats(string playername) {
            EcoStats es = default(EcoStats);
            es.playerName = playername;
            using (DataTable eco = Database.Fill("SELECT * FROM Economy WHERE player=@0", playername)) {
                if (eco.Rows.Count >= 1) {
                    es.money = int.Parse(eco.Rows[0]["money"].ToString());
                    es.totalSpent = int.Parse(eco.Rows[0]["total"].ToString());
                    es.purchase = eco.Rows[0]["purchase"].ToString();
                    es.payment = eco.Rows[0]["payment"].ToString();
                    es.salary = eco.Rows[0]["salary"].ToString();
                    es.fine = eco.Rows[0]["fine"].ToString();
                } else {
                    es.purchase = "%cNone";
                    es.payment = "%cNone";
                    es.salary = "%cNone";
                    es.fine = "%cNone";
                }
            }
            return es;
        }

        public static void UpdateEcoStats(EcoStats es) {
            string type = Server.useMySQL ? "REPLACE INTO" : "INSERT OR REPLACE INTO";
            Database.Execute(type + " Economy (player, money, total, purchase, payment, salary, fine) " +
                             "VALUES (@0, @1, @2, @3, @4, @5, @6)", es.playerName, es.money, es.totalSpent,
                             es.purchase, es.payment, es.salary, es.fine);
        }
        
        public static Item[] Items = { new ColorItem(), new TitleColorItem(), 
            new TitleItem(), new RankItem(), new LevelItem(), new LoginMessageItem(), 
            new LogoutMessageItem(), new BlocksItem(), new QueueLevelItem(), 
            new InfectMessageItem(), new NickItem(), new InvisibilityItem(), new ReviveItem() };
        
        public static Item GetItem(string name) {
            foreach (Item item in Items) {
                if (name.CaselessEq(item.Name)) return item;
                
                foreach (string alias in item.Aliases) {
                    if (name.CaselessEq(alias)) return item;
                }
            }
            return null;
        }
            
        public static string GetItemNames() {
            string items = Items.Join(x => x.Enabled ? x.ShopName : null);
            return items.Length == 0 ? "(no enabled items)" : items;
        }
        
        public static SimpleItem Color { get { return (SimpleItem)Items[0]; } }
        public static SimpleItem TitleColor { get { return (SimpleItem)Items[1]; } }
        public static SimpleItem Title { get { return (SimpleItem)Items[2]; } }
        public static RankItem Ranks { get { return (RankItem)Items[3]; } }
        public static LevelItem Levels { get { return (LevelItem)Items[4]; } }
        
        public static void MakePurchase(Player p, int cost, string item) {
            Economy.EcoStats ecos = RetrieveEcoStats(p.name);
            p.SetMoney(p.money - cost);
            ecos.money = p.money;
            ecos.totalSpent += cost;
            ecos.purchase = item + "%3 - Price: %f" + cost + " %3" + Server.moneys +
                " - Date: %f" + DateTime.Now.ToString(CultureInfo.InvariantCulture);
            UpdateEcoStats(ecos);
            Player.Message(p, "%aYour balance is now %f" + p.money + " %3" + Server.moneys);
        }
    }
}