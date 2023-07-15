using System;
using BepInEx;
using UnityEngine;
using System.Linq;
using RWCustom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static SlugBase.Features.FeatureTypes;
using SlugBase.Features;
using SlugBase;
using SlugBase.DataTypes;

namespace TheNymph
{
    [BepInPlugin(MOD_ID, "The Nymph", "1.0.1")]
    public partial class Nymphmod : BaseUnityPlugin
    {
        //Housekeeping and setup stuff
        private const string MOD_ID = "tyxaar.nymph";
        public static SlugcatStats.Name Nymph = new SlugcatStats.Name("Nymph");


        //Grab the colour features
        public static readonly PlayerFeature<ColorSlot[]> CustomColors;

        //Set up the CWT for multiplayer compatibility
        static ConditionalWeakTable<Player, PlayerValues> PlayerData = new ConditionalWeakTable<Player, PlayerValues>();
        static PlayerValues Data(Player player) => PlayerData.GetValue(player, p => new PlayerValues(p));

        //Variables
        public bool startSequence = true;

        // Add hooks
        void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Custom hooks!
            On.Player.ctor += Player_Ctor;
            On.Player.Jump += Player_Jump;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.PlayerGraphics.ctor += PlayerGraphics_Ctor; ;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            //On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
            On.RainWorldGame.IsMoonActive += RainWorldGame_IsMoonActive;
            On.RainWorldGame.MoonHasRobe += RainWorldGame_MoonHasRobe;
            On.SaveState.ctor += SaveState_Ctor;
        }

        //Add save-persistent data
        void SaveState_Ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (self.saveStateNumber == Nymph)
            {
                self.deathPersistentSaveData.theMark = true;
                self.theGlow = true;
            }
        }

        //Revive Moon pt. 1
        bool RainWorldGame_MoonHasRobe(On.RainWorldGame.orig_MoonHasRobe orig, RainWorldGame self)
        {
            if (self.StoryCharacter == Nymph && ((Player)self.Players.First().realizedCreature).slugcatStats.name == Nymph && self.IsStorySession)
            {
                return true;
            }
            return orig(self);
        }

        //Revive Moon pt. 2, electric boogaloo
        bool RainWorldGame_IsMoonActive(On.RainWorldGame.orig_IsMoonActive orig, RainWorldGame self)
        {
            if (self.StoryCharacter == Nymph &&((Player)self.Players.First().realizedCreature).slugcatStats.name == Nymph && self.GetStorySession.saveState.miscWorldSaveData.SLOracleState.neuronsLeft > 0)
            {
                return true;
            }
            return orig(self);
        }

        //Initiate new game script
        public void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);
            if (room.game.session is StoryGameSession && (room.game.session as StoryGameSession).saveState.cycleNumber == 0 && room.abstractRoom.name == "SI_A07" && startSequence == true)
            {
                room.AddObject(new NymphStart(room));
                startSequence = false;
            }
        }

        //Change spear damage and speed
        public void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (self.SlugCatClass == Nymph)
            {
                spear.spearDamageBonus = 0.5f;
                BodyChunk firstChunk = spear.firstChunk;
                firstChunk.vel.x *= 4f;
            }
        }

        //MovementUpdate hook
        public void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.SlugCatClass == Nymph)
            {
                var player = Data(self);

                //const float flightSpeed = 0.01f;
                const float normalGravity = 0.9f;
                const float boostPower = 15;
                const int zeroGFoodCount = 8;
                const int normalFoodCount = 4;


                Vector2 vel = self.bodyChunks[0].vel;
                if (self.bodyChunks[1].vel.magnitude < vel.magnitude)
                {
                    vel = self.bodyChunks[1].vel;
                }

                //Cooldown for how soon you can jump afte getting off the ground. Mostly used to fix 0g problems.
                if (player.glideCooldown < 5 && (player.rechargeGlide || (self.bodyMode == Player.BodyModeIndex.ZeroG && self.animation == Player.AnimationIndex.ZeroGSwim)))
                {
                    player.glideCooldown = 5;
                }

                //Flap in 0g, copied from the Artificer code.
                if (self.FoodInStomach > 0 && player.glideinput && self.bodyMode == Player.BodyModeIndex.ZeroG && self.animation == Player.AnimationIndex.ZeroGSwim && player.glideCooldown == 0)
                {
                    //Check if food pips need to be drained
                    player.foodCountdown++;

                    //Make fwoosh sound
                    self.room.PlaySound(SoundID.Vulture_Wing_Woosh_LOOP, self.mainBodyChunk.pos, 1, 2.5f);

                    //Set cooldown
                    player.glideCooldown = 40;

                    //Leap!
                    float jumpX = self.input[0].x;
                    float jumpY = self.input[0].y;
                    while (jumpX == 0f && jumpY == 0f)
                    {
                        jumpX = (UnityEngine.Random.value <= 0.33) ? 0 : ((UnityEngine.Random.value <= 0.5) ? 1 : -1);
                        jumpY = (UnityEngine.Random.value <= 0.33) ? 0 : ((UnityEngine.Random.value <= 0.5) ? 1 : -1);
                    }
                    self.bodyChunks[0].vel.x = 9f * jumpX;
                    self.bodyChunks[0].vel.y = 9f * jumpY;
                    self.bodyChunks[1].vel.x = 8f * jumpX;
                    self.bodyChunks[1].vel.y = 8f * jumpY;
                }

                //Initiate flap 
                if (player.triggerGlide && self.bodyMode != Player.BodyModeIndex.ZeroG)
                {
                    if (self.input[0].pckp && self.FoodInStomach > 0)
                    {
                        player.foodCountdown++;

                        //make fwoosh sound
                        player.canFlap = false;
                        self.room.PlaySound(SoundID.Vulture_Wing_Woosh_LOOP, self.mainBodyChunk.pos, 1, 2.5f);
                        //Sparks!
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 a = Custom.RNV();
                            self.room.AddObject(new Spark(self.mainBodyChunk.pos + a * UnityEngine.Random.value * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                        }

                        //Boost!
                        foreach (BodyChunk chunk in self.bodyChunks)
                        {
                            chunk.vel.y = boostPower;
                        }
                    }
                    //Set up variables for gliding.
                    player.canGlide = true;
                    player.playerGliding = true;
                }

                //If statement that runs when gliding.
                if (player.canGlide && player.playerGliding && self.mainBodyChunk.vel.y < 0f && player.glideTimer > 0 && !player.touchingTerrain)
                {
                    //Apply glide physics changes
                    self.gravity = player.flightGravity;
                    self.customPlayerGravity = player.flightGravity;

                    self.bodyChunks[0].vel.y += 2f;
                    self.bodyChunks[1].vel.y -= 1f;

                    //Sparks that show when your glide is running out
                    if (player.glideTimer > 0 && player.glideTimer < 40)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 a = Custom.RNV();
                            self.room.AddObject(new Spark(self.mainBodyChunk.pos + a * UnityEngine.Random.value * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                        }
                    }

                    player.glideTimer--;
                    if (player.glideTimer == 0 || (player.glideinput && player.playerGliding))
                    {
                            player.playerGliding = false;
                            player.glideTimer = 0;
                            Debug.Log("Exit glide");
                    }
                }

                //Recharge the glide when on a wall, floor, or pole.
                if (player.rechargeGlide && self.room.gravity != 0)
                {
                    player.glideTimer = 80;
                    self.gravity = normalGravity;
                    self.customPlayerGravity = normalGravity;
                    player.canFlap = true;
                    player.playerGliding = false;
                    player.canGlide = true;
                }

                //Make glide cooldown tick down.
                if (player.glideCooldown > 0)
                {
                    player.glideCooldown--;
                }


                if ((player.foodCountdown == normalFoodCount && self.room.gravity != 0) && (self.room.gravity == 0 && player.foodCountdown == zeroGFoodCount))
                {
                    self.SubtractFood(1);
                    player.foodCountdown = 0;
                }

                //Logs
                //Debug.Log("canGlide? " + player.canGlide);
                //Debug.Log("Player gliding? " + player.playerGliding);
                //Debug.Log("pressing hold? " + self.input[0].pckp);
                //Debug.Log("Input y = "+ self.input[0].y);
                //Debug.Log("y velocity "+ self.mainBodyChunk.vel.y);
            }
        }

        //Make Nymph's pole jumps big
        void Player_Jump(On.Player.orig_Jump orig, Player self)
            {
                if (self.slugcatStats.name == Nymph)
                {
                    self.jumpBoost *= 1.2f;
                    if (self.animation == Player.AnimationIndex.ClimbOnBeam)
                    {
                        self.jumpBoost *= 0f;
                        float num = Mathf.Lerp(1f, 1.15f, self.Adrenaline);
                        if (self.input[0].x != 0)
                        {
                            self.animation = Player.AnimationIndex.None;
                            self.bodyChunks[0].vel.y = 9f * num;
                            self.bodyChunks[1].vel.y = 8f * num;
                            self.bodyChunks[0].vel.x = 9f * self.flipDirection * num;
                            self.bodyChunks[1].vel.x = 7f * self.flipDirection * num;
                        }
                    }
                }
                orig(self);
                if (self.slugcatStats.name == Nymph && (self.animation != Player.AnimationIndex.ClimbOnBeam))
                {
                    self.jumpBoost *= 1.2f;
                }
            }

        //Player info
        void Player_Ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.slugcatStats.name == Nymph)
            {
                (abstractCreature.state as PlayerState).isPup = true;
            }
        }

        //Script that runs when starting a new game
        public class NymphStart : UpdatableAndDeletable
        {
            private int Timer = 0;
            private Player Nymph => (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);
            public NymphStart(Room room)
            {
                this.room = room;
            }
            public override void Update(bool eu)
            {
                if (Nymph == null) return;
                Player player = Nymph;
                base.Update(eu);
                player.playerState.foodInStomach = 3;
                for (int i = 0; i < 2; i++)
                {
                    player.bodyChunks[i].HardSetPosition(room.MiddleOfTile(24, 90));
                    player.bodyChunks[i].vel = new Vector2(0, 0);
                }
                if (Timer == 160)
                {
                    player.bodyChunks[0].vel = new Vector2(0, 0);
                    player.bodyChunks[1].vel = new Vector2(0, 0);
                    Destroy();
                }
                Timer++;
            }

        }

    }
}