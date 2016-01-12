using GTA;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

public class ScriptTutorial : Script
{
    int max_companions = 3;
    List<Ped> group_members = new List<Ped>();

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
            return;
        }

        foreach (Ped p in group_members)
        {
            if (p.IsDead)
            {
                group_members.Remove(p);
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

            companion.IsInvincible = true;
            companion.Accuracy = 100;
            companion.Weapons.Give(GTA.Native.WeaponHash.AdvancedRifle, 5000, true, true);
            companion.Weapons.Give(GTA.Native.WeaponHash.MicroSMG, 5000, true, true);
            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_COMBAT_ABILITY, companion, 2);

            companion.RelationshipGroup = player.RelationshipGroup;
            group_members.Add(companion);
            int player_group = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_PED_GROUP_INDEX, player);
            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_AS_GROUP_MEMBER, companion, player_group);

            GTA.Native.Function.Call(GTA.Native.Hash.TASK_COMBAT_HATED_TARGETS_IN_AREA, companion, 50000, 0);
            GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_KEEP_TASK, companion, true);
        }

        if (e.KeyCode == Keys.J)
        {
            group_members[group_members.Count - 1].Kill();
            group_members.RemoveAt(group_members.Count - 1);
        }
    }
}