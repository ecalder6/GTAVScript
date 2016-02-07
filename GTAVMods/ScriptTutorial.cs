using GTA;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using GTA.Native;

public class ScriptTutorial : Script
{
    private int max_companions = 300;
    private List<Ped> group_members = new List<Ped>();
    private List<GunGamePlayer> GunPlayers = new List<GunGamePlayer>();
    private bool gunGameStarted = false;
    private List<GunGameWeapon> weaponList = new List<GunGameWeapon>();
    private int weaponID;
    private int weaponScore = 14;

    public struct GunGameWeapon
    {
        public WeaponHash weapon;

        public int ammo;

        public int scoreLimit;

        public int id;


        public GunGameWeapon(WeaponHash gun, int s, int a, int i)
        {
            this.weapon = gun;
            this.scoreLimit = s;
            this.ammo = a;
            this.id = i;
        }
    }

    public struct GunGamePlayer
    {
        public Ped player;
        public GunGameWeapon weapon;
        public int score;

        public GunGamePlayer(Ped p, GunGameWeapon w, int s)
        {
            this.player = p;
            this.weapon = w;
            this.score = s;

            this.setWeapon(w);
        }

        public void incScore()
        {
            this.score++;
        }

        public Ped getPlayer()
        {
            return this.player;
        }

        public void setWeapon(GunGameWeapon w)
        {
            this.weapon = w;
            this.player.Weapons.RemoveAll();
            this.player.Weapons.Give(w.weapon, w.ammo, true, true);
        }
    }

    public ScriptTutorial()
    {
        Tick += OnTick;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        Interval = 100;
    }

    void OnTick(object sender, EventArgs e)
    {
        Player player = Game.Player;

        if (player.IsDead)
        {
            foreach (Ped p in group_members)
            {
                if (p.IsAlive) {
                    p.Kill();
                }
                group_members.Remove(p);
            }

            if (gunGameStarted)
            {
                /*Logger.Log("Player died");
                this.gunGameStarted = false;
                this.endGunGame(player.Character);*/
            }
        }

        foreach (Ped p in group_members)
        {
            if (p.IsDead)
            {
                group_members.Remove(p);

            }
        }

        if (player.IsAiming)
        {
            Entity entity = this.getEntityAimedByPlayer();
            if (entity != null)
            {
                if (entity.GetType() == typeof(GTA.Ped))
                {

                    foreach (Ped p in group_members)
                    {
                        if (!p.IsDead)
                        {
                            p.Task.ClearAllImmediately();
                            p.Task.FightAgainst((GTA.Ped) entity, 150000);
                            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_KEEP_TASK, p, true);

                        }
                    }
                }
            }
        }

        if (gunGameStarted)
        {
            //Added surronding NPC's to the gungame
            Ped[] nearbyPeds = World.GetNearbyPeds(Game.Player.Character, 10000);
            foreach (Ped p in nearbyPeds)
            {
                if (!p.IsAlive || p == Game.Player.Character)
                {
                    continue;
                }

                bool inGame = false;
                foreach (GunGamePlayer g in GunPlayers)
                {
                    if (g.getPlayer() == p)
                    {
                        inGame = true;
                        break;
                    }
                }

                if (inGame)
                {
                    continue;
                }

                p.Task.ClearAllImmediately();
                GunGamePlayer gp = new GunGamePlayer(p, this.weaponList[0], 0);
                GunPlayers.Add(gp);
                p.Task.FightAgainst(Game.Player.Character);
                p.AlwaysKeepTask = true;
            }

            //Check if any player is eligible for upgrade and upgrade them.
            //if Any NPC dies, remove them from list.
            for (int i = 0; i < GunPlayers.Count; i++)
            {
                GunGamePlayer g = GunPlayers[i];
                if (!g.player.IsAlive)
                {
                    Logger.Log("Someone was killed");

                    Entity killer = g.player.GetKiller();

                    if (killer != null && killer.GetType() == typeof(Ped))
                    {

                        if (Game.Player.Character == killer)
                        {
                            GunGamePlayer gg = new GunGamePlayer(Game.Player.Character, weaponList[GunPlayers[0].weapon.id], GunPlayers[0].score + 1);
                            GunPlayers[0] = gg;
                            Logger.Log(gg.score);
                        }
                        /*for (int j = 0; j < GunPlayers.Count; j++)
                        {
                            GunGamePlayer gg = GunPlayers[j];
                            if (gg.player == (Ped) killer)
                            {
                                GunGamePlayer newg = new GunGamePlayer(gg.player, weaponList[gg.weapon.id], gg.score + 1);
                                GunPlayers[j] = newg;
                                if ((Ped) killer == Game.Player.Character)
                                {
                                    Logger.Log(gg.score);
                                }
                                break;
                            }
                        }*/
                    }
                    GunPlayers.RemoveAt(i);
                }

                if (g.score >= weaponScore)
                {
                    Logger.Log("ezpz");
                    endGunGame(g.getPlayer());
                }
                
                if (g.score >= g.weapon.scoreLimit)
                {
                    GunGamePlayer gg = new GunGamePlayer(g.player, weaponList[g.weapon.id + 1], GunPlayers[i].score);
                    GunPlayers[i] = gg;
                    Logger.Log("upgraded");
                }
            }
        }
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {

    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.H && group_members.Count < max_companions)
        {
            Ped player = Game.Player.Character;
            GTA.Math.Vector3 spawnLoc = player.Position + player.ForwardVector * 5;

            List<string> model_names = new List<string>();
            model_names.Add("S_M_Y_Marine_03");

            Ped companion = GTA.World.CreatePed(model_names[0], spawnLoc);
            companion.Task.ClearAllImmediately();

            //companion.IsInvincible = true;
            companion.Accuracy = 100;
            companion.Weapons.Give(GTA.Native.WeaponHash.RPG, 50000, true, true);
            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_COMBAT_ABILITY, companion, 2);

            companion.RelationshipGroup = player.RelationshipGroup;
            group_members.Add(companion);
            companion.IsInvincible = true;
            int player_group = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_PED_GROUP_INDEX, player);
            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_AS_GROUP_MEMBER, companion, player_group);

            //GTA.Native.Function.Call(GTA.Native.Hash.TASK_COMBAT_HATED_TARGETS_IN_AREA, companion, 50000, 0);
            //GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_KEEP_TASK, companion, true);
        }

        if (e.KeyCode == Keys.J)
        {
            group_members[group_members.Count - 1].Kill();
            group_members.RemoveAt(group_members.Count - 1);
        }

        if (e.KeyCode == Keys.O)
        {
            Ped[] nearbyPeds = World.GetNearbyPeds(Game.Player.Character, 50000);
            foreach (Ped p in nearbyPeds) {
                p.ApplyForceRelative(p.UpVector * 5);
            }
        }

        if (e.KeyCode == Keys.G)
        {
            if (!this.gunGameStarted) {

                this.populateWeapons();

                GunGamePlayer gp = new GunGamePlayer(Game.Player.Character, weaponList[0], 0);
                Logger.Log("should only show once");

                GunPlayers.Add(gp);

                this.gunGameStarted = true;

                UI.ShowSubtitle("Gun Game Starts!", 5000);
            }
        }

    }

    private Entity getEntityAimedByPlayer()
    {
        OutputArgument output = new OutputArgument();
        if (Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, Game.Player, output))
        {
            return output.GetResult<Entity>();
        }
        else
        {
            return null;
        }
    }

    private void populateWeapons()
    {
        weaponID = 0;
        this.weaponList.Add(new GunGameWeapon(WeaponHash.AssaultRifle, 0, 5000, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.CarbineRifle, 2, 30, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.SpecialCarbine, 4, 30, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.AssaultShotgun, 6, 15, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.SniperRifle, 8, 5, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.CombatPistol, 10, 30, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.Pistol, 12, 30, weaponID++));
        this.weaponList.Add(new GunGameWeapon(WeaponHash.Knife, 14, 0, weaponID++));
    }

    public void endGunGame(Ped winner)
    {
        gunGameStarted = false;

        Logger.Log("won game");

        if (winner == Game.Player.Character && GunPlayers[0].score >= weaponScore)
        {
            UI.ShowSubtitle("Congratulations! You won!", 5000);
        }
        else if (winner == Game.Player.Character)
        {
            UI.ShowSubtitle("You lost the gun game.", 5000);
        } 
        else
        {
            UI.ShowSubtitle("An NPC won!", 5000);
        }

        if (GunPlayers.Count == 0)
        {
            return;
        }

        GunPlayers.RemoveAt(0);
        foreach (GunGamePlayer g in GunPlayers)
        {
            g.getPlayer().Kill();
            GunPlayers.Remove(g);
        }
    }
}