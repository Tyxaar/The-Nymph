using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Collections.Generic;
using System.Linq;
using RWCustom;using System.Security;
using System.Security.Permissions;


namespace TheNymph
{
    [BepInPlugin(MOD_ID, "The Nymph", "1.0.1")]
    class Plugin : BaseUnityPlugin
    {
        //Housekeeping and setup stuff
        private const string MOD_ID = "tyxaar.nymph";
        static SlugcatStats.Name NymphClass = new SlugcatStats.Name("Nymph");

        bool playerGliding;
        int glideTimer = 40;


        // Add hooks
        void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Jump += Player_Jump;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.PlayerGraphics.ctor += PlayerGraphics_Ctor;;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.Player.ctor += Player_Ctor;
            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
        }

        void PlayerGraphics_Ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);

            if (self.player.slugcatStats.name == NymphClass)
            {
                self.tail = new TailSegment[6];
                self.tail[0] = new TailSegment(self, 6f, 4f, null, 0.1f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 5f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 3f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 2.5f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
                self.tail[4] = new TailSegment(self, 1f, 7f, self.tail[3], 0.85f, 1f, 0.5f, true);
                self.tail[5] = new TailSegment(self, 0.5f, 7f, self.tail[4], 0.85f, 1f, 0.5f, true);
                var bp = self.bodyParts.ToList();
                bp.RemoveAll(x => x is TailSegment);
                bp.AddRange(self.tail);

                self.bodyParts = bp.ToArray();
            }
        }


        void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (self.player.slugcatStats.name == NymphClass)
            {
                foreach (var hnd in self.hands)
                {
                    if (self.player.bodyMode == Player.BodyModeIndex.Stand || self.player.animation == Player.AnimationIndex.BeamTip)
                    {
                            hnd.pos.y = Mathf.Lerp(hnd.lastPos.y - 4f, hnd.pos.y - 4f, 1f);
                    }
                    else if (self.player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                    {
                        hnd.pos.y = Mathf.Lerp(hnd.lastPos.y - 8f, hnd.pos.y - 8f, 1f);
                    }
                }

            } 
        }

        public void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);
            if (room.game.session is StoryGameSession && (room.game.session as StoryGameSession).saveState.cycleNumber == 0 && room.abstractRoom.name == "SI_A07")
            {
                room.AddObject(new NymphStart(room));
            }
        }

        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }

        public void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            spear.spearDamageBonus = 0.75f;
            BodyChunk firstChunk = spear.firstChunk;
            firstChunk.vel.x = firstChunk.vel.x * 2f;
        }

        public void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (self.SlugCatClass == NymphClass)
            {
                if (self.bodyMode != Player.BodyModeIndex.CorridorClimb)
                {
                    self.bodyChunkConnections[0].distance = 12f;
                } else {
                    self.bodyChunkConnections[0].distance = 17f;
                }

                orig(self, eu);
                bool grabbingGrappleworm = (self.grasps[0]?.grabbed is TubeWorm || self.grasps[1]?.grabbed is TubeWorm);
                bool touchingTerrain = (self.bodyChunks[0].contactPoint != default || self.bodyChunks[1].contactPoint != default || self.canWallJump != 0 && self.canJump > 0 || self.bodyMode == Player.BodyModeIndex.Stand || self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || self.bodyMode == Player.BodyModeIndex.WallClimb || self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.Swimming || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam);
                bool playerStartGlide = (!touchingTerrain) && self.canJump <= 0 && self.input[0].jmp && !self.input[1].jmp;

                if (playerStartGlide && !playerGliding && (glideTimer > 0) && !grabbingGrappleworm)
                {

                    self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.mainBodyChunk.pos);
                    //Starting the Glide
                    foreach (var chunk in self.bodyChunks)
                    {
                        chunk.vel.y = 0f;
                    }
                    playerGliding = true;
                }
                if (playerGliding == true && glideTimer > 0)
                {
                    self.customPlayerGravity = 0.01f;
                    glideTimer--;
                } else {
                    self.customPlayerGravity = 1f;
                    playerGliding = false;

                }
                if (glideTimer > 0 && glideTimer < 10)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 a = Custom.RNV();
                        self.room.AddObject(new Spark(self.mainBodyChunk.pos + a * UnityEngine.Random.value * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                    }
                }
                if (touchingTerrain)
                {
                    playerGliding = false;
                    self.customPlayerGravity = 1f;
                    glideTimer = 40;
                }
                /*Debug.Log(glideTimer);
                Debug.Log(playerGliding);
                Debug.Log(self.gravity);*/
            }
        }

        void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (self.SlugCatClass == NymphClass)
            {
                self.jumpBoost *= 1.3f;
            }
        }

        void Player_Ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (self.slugcatStats.name == NymphClass) {

                (abstractCreature.state as PlayerState).isPup = true;
            }
        }

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
                Player ply = Nymph;
                base.Update(eu);
                ply.playerState.foodInStomach = 3;
                for (int i = 0; i < 2; i++)
                {
                    ply.bodyChunks[i].HardSetPosition(room.MiddleOfTile(24, 80));
                    ply.bodyChunks[i].vel = new Vector2(0, 0);
                }
                if (Timer == 160)
                {
                    ply.bodyChunks[0].vel = new Vector2(0, 0);
                    ply.bodyChunks[1].vel = new Vector2(0, 0);
                    Destroy();
                }
                else if (Timer > 160 || Timer < 300)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 a = Custom.RNV();
                        ply.room.AddObject(new Spark(ply.mainBodyChunk.pos + a * UnityEngine.Random.value * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                    }
                } else if (Timer == 360)
                {
                    Destroy();
                }
                Timer++;
            }
        }

    }
}