using BepInEx;
using UnityEngine;
using SlugBase;
using System.IO;
using System.Collections.Generic;

namespace Nymph
{
    [BepInPlugin("author.Tyxaar", "The Nymph", "0.95")]	// (GUID, mod name, mod version)
    public class TheNymph : BaseUnityPlugin
    {
        public void OnEnable()
        {
            // This is called when the mod is loaded.
            PlayerManager.RegisterCharacter(new Nymph());
        }

         public class Nymph : SlugBaseCharacter
        {
            public Nymph() : base("The Nymph", FormatVersion.V1, 2) { }
            public override string Description => "The Nymph is a small and agile thing, but possesses vastly inferior strength.\n Use your agility and strange powers to traverse this world.";
            Dictionary<Player, int> glideTimers = new Dictionary<Player, int>();

            protected override void Enable()
            {
                On.Player.Jump += Player_Jump;
                On.Player.ShortCutColor += Player_ShortCutColor;
                On.Player.MovementUpdate += Player_MovementUpdate;
                On.Player.ctor += Player_ctor;
                On.Player.ThrownSpear += Player_ThrownSpear;
                On.PlayerGraphics.ctor += PlayerGraphics_ctor;
                //On.WorldLoader.NextActivity += WorldLoader_NextActivity;
            }

            private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
            {
                orig(self, ow); 
                self.drawPositions[0, 0] = Vector2.Lerp(self.drawPositions[0, 0], self.drawPositions[1, 0], 0.4f);
            }

            public override Stream GetResource(params string[] path)
            {
                return typeof(Nymph).Assembly.GetManifestResourceStream(typeof(Nymph), "SlugBase." + string.Join(".", path));
            }

            public override void StartNewGame(Room room)
            {
                base.StartNewGame(room);

                // Make sure this is the right room
                if (room.abstractRoom.name != StartRoom) return;

                room.AddObject(new NymphStart(room));
            }
            public override string StartRoom => "SI_A07";

            public void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
            {
                orig(self, spear);
                if (IsMe(self))
                {
                    spear.spearDamageBonus = 0.75f;
                    BodyChunk firstChunk = spear.firstChunk;
                    firstChunk.vel.x = firstChunk.vel.x * 1.35f;
                }
            }
            bool playerGliding;
            public void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
            {
                orig(self, abstractCreature, world); 
                self.spearOnBack = new Player.SpearOnBack(self);
            //The timer for how long you can glide, 40 ticks = 1 second
                glideTimers[self] = 40;
            }
            public void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
            {
                int myGlideTimer = glideTimers[self];
                orig(self, eu);
                //Make the player smol

                self.bodyChunkConnections[0].distance = 12f;
                if (self.bodyMode == Player.BodyModeIndex.CorridorClimb)
                {
                    self.bodyChunkConnections[0].distance = 17f;
                }

                //Weather the player is holding a grappleworm or not.
                bool grabbingGrappleworm = (self.grasps[0]?.grabbed is TubeWorm || self.grasps[1]?.grabbed is TubeWorm);

                //Weather the player is touching terrain
                bool touchingTerrain = (self.bodyChunks[0].contactPoint != default || self.bodyChunks[1].contactPoint != default || self.canWallJump != 0 && self.canJump > 0 || (self.bodyMode == Player.BodyModeIndex.Stand || self.bodyMode == Player.BodyModeIndex.Crawl || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || self.bodyMode == Player.BodyModeIndex.WallClimb || self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.Swimming));

                //Weather the player has pressed jump while in the air (the glide trigger)
                bool playerStartGlide = (!touchingTerrain) && self.canJump <= 0 && self.input[0].jmp && !self.input[1].jmp;

                if (playerStartGlide && !playerGliding && (myGlideTimer > 0) && !grabbingGrappleworm)
                {
                    //Starting the Glide
                    foreach (var chunk in self.bodyChunks)
                    {
                        chunk.vel *= 0.2f;
                    }
                    playerGliding = true;

                }
                else if (playerGliding && (myGlideTimer > 0) && !touchingTerrain)
                {
                    //Player is gliding
                    self.gravity = 0.01f;
                    myGlideTimer -= 1;
                }
                else if (playerStartGlide && playerGliding)
                {
                       myGlideTimer = 0;
                }
                else {
                    //Player is not gliding
                    self.gravity = 0.9f;
                    playerGliding = false;
                }
                if (self.bodyMode == Player.BodyModeIndex.Stand)
                {
                    //Player is not gliding
                    self.gravity = 0.9f;
                    playerGliding = false;
                    myGlideTimer = 40;
                }
                if (myGlideTimer <= 0)
                {
                    //Player is not gliding
                    self.gravity = 1f;
                    playerGliding = false;
                }
                //Debug.Log(myGlideTimer);
                //Debug.Log(playerGliding);
                glideTimers[self] = myGlideTimer;

            }


            //The colours of the slugcat's body, eyes, and pipe sprite respectively.
            public override Color? SlugcatColor() => new Color(0.54f, 0.42f, 0.73f);
            public override Color? SlugcatEyeColor() => new Color(0.1f, 0f, 0.2f);
            private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self) => new Color(0.54f, 0.42f, 0.73f);

            protected override void GetStats(SlugcatStats stats)
            {
                stats.runspeedFac *= 1.5f;
                stats.poleClimbSpeedFac *= 2f;
                stats.corridorClimbSpeedFac *= 2f;
                stats.loudnessFac *= 0.6f;
                stats.bodyWeightFac *= 0.9f;
                stats.lungsFac *= 0.75f;
            }
            private static void Player_Jump(On.Player.orig_Jump orig, Player self)
            {
                orig(self);
                self.jumpBoost *= 1.25f;
            }
            public override void GetFoodMeter(out int maxFood, out int foodToSleep)
            {
                maxFood = 7;
                foodToSleep = 6;
            }
            public override bool CanEatMeat(Player player, Creature creature) => true;
            public override bool QuarterFood => true;

            protected override void Disable()
            {
                On.Player.Jump -= Player_Jump;
                On.Player.ShortCutColor -= Player_ShortCutColor;
                On.Player.MovementUpdate -= Player_MovementUpdate;
                On.Player.ctor -= Player_ctor;
                On.Player.ThrownSpear -= Player_ThrownSpear;
                On.PlayerGraphics.ctor -= PlayerGraphics_ctor;
                glideTimers.Clear();
            }
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
            for (int i = 0; i< 2; i++)
                {
                    ply.bodyChunks[i].HardSetPosition(room.MiddleOfTile(24, 80));
                ply.bodyChunks[i].vel = new Vector2(0 , 0);
                }
            if (Timer == 160)
            {
                ply.bodyChunks[0].vel = new Vector2(0, 0);
                ply.bodyChunks[1].vel = new Vector2(0, 0);
                Destroy();
            }
            Timer++;
        }
    }

}



