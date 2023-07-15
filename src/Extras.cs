using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;

/*
 * This file contains fixes to some common problems when modding Rain World.
 * Unless you know what you're doing, you shouldn't modify anything here.
 */

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TheNymph
{
    sealed class PlayerValues
    {
        public WeakReference<Player> playerRef;
        public int glideTimer;
        public int glideCooldown = 80;
        public int foodCountdown;
        public bool playerGliding;
        public bool canFlap;
        public bool canGlide;

        public Color tailColour;
        public Color bodyColour;

        public bool glideinput => playerRef.TryGetTarget(out var player) && player.input[0].jmp && !player.input[1].jmp;

        public SlugBaseCharacter Character;

        public bool holdingTubeworm => playerRef.TryGetTarget(out var player) && player != null && player.grasps[0]?.grabbed is TubeWorm || player.grasps[1]?.grabbed is TubeWorm;
        
        public List<Player.AbstractOnBackStick> abstractBackSpears = new List<Player.AbstractOnBackStick>();
        public List<Spear> backSpears = new List<Spear>();
       
        public bool rechargeGlide
        {
            get
            {
                playerRef.TryGetTarget(out var player);
                string[] bodyModes = { "Stand", "CorridorClimb", "WallClimb", "Swimming", "ClimbingOnBeam"};
                for (int i = 0; i < bodyModes.Length; i++)
                {
                    Player.BodyModeIndex bodyRef = new Player.BodyModeIndex(bodyModes[i]);
                    if (player.bodyMode == bodyRef)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool touchingTerrain => playerRef.TryGetTarget(out var player) && player != null && (player.bodyChunks[0].contactPoint != default || player.bodyChunks[1].contactPoint != default && player.bodyMode != Player.BodyModeIndex.Default && player.animation != Player.AnimationIndex.Flip) || rechargeGlide;
        public bool triggerGlide => playerRef.TryGetTarget(out var player) && glideCooldown == 0 && glideTimer > 0 && !touchingTerrain && canFlap && glideinput;
                

        public float flightGravity
        {
            get
            {
                playerRef.TryGetTarget(out var player);
                return player.room.gravity * 0.01f;
            }
        }

        public PlayerValues(Player self)
        {
            playerRef = new WeakReference<Player>(self);
        }

        private void SetupColours(Player self)
        {
            SlugcatStats.Name Name = self.slugcatStats.name;

            if (SlugBaseCharacter.TryGet(Name, out Character))
            {
                if (Character.Features.TryGet(PlayerFeatures.CustomColors, out var customColors))
                {
                    var playerNumber = !self.room.game.IsArenaSession && self.playerState.playerNumber == 0 ? -1 : self.playerState.playerNumber;
                    bodyColour = customColors[0].GetColor(playerNumber);
                    tailColour = customColors[2].GetColor(playerNumber);
                }
            }
        }



    }
}

internal static class Extras
{
    private static bool _initialized;

    // Ensure resources are only loaded once and that failing to load them will not break other mods
    public static On.RainWorld.hook_OnModsInit WrapInit(Action<RainWorld> loadResources)
    {
        return (orig, self) =>
        {
            orig(self);

            try
            {
                if (!_initialized)
                {
                    _initialized = true;
                    loadResources(self);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        };
    }
}