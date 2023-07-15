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
    public partial class Nymphmod
    {

        //Load resources
        private void LoadResources(RainWorld rainWorld)
        {
            Futile.atlasManager.LoadAtlas("sprites/NymphHead");
            Futile.atlasManager.LoadAtlas("sprites/NymphFace");
            Futile.atlasManager.LoadAtlas("sprites/NymphTail");
            Futile.atlasManager.LoadAtlas("sprites/NymphTail");
        }
        private static readonly string[] ValidSpriteNames = new[] { "Head", "Face", "PFace", "BodyA", "HipsA", "PlayerArm", "Legs", "Tail", "Futile_White" };

        private const string NoirHead = "NymphHead";
        private const string NoirEars = "NoirEars";
        private const string NymphFace = "NymphFace";
        private const string NymphTail = "NymphTail";
        private const string NymphPrfx = "Nymph"; //Prefix for sprite replacement




        private const int HeadSpr = 3;
        private const int FaceSpr = 9;
        private const int TailSpr = 2;

        private List<int> SprToReplace = new List<int>()
        {HeadSpr, FaceSpr, TailSpr};
        private const int TailLength = 7;

        private void ReplaceSprites(RoomCamera.SpriteLeaser sleaser, PlayerGraphics self)
        {
            foreach (var num in SprToReplace)
            {
                var spr = sleaser.sprites[num].element;

                if (!spr.name.StartsWith("Nymph"))
                {
                    if (!ValidSpriteNames.Any(spr.name.StartsWith)) //For DMS compatibility :)
                    {
                        continue;
                    }

                    if (num == TailSpr)
                    {
                        sleaser.sprites[num].element = Futile.atlasManager.GetElementWithName(NymphTail);
                    }
                    else
                    {
                        sleaser.sprites[num].element = Futile.atlasManager.GetElementWithName("Nymph" + spr.name);
                    }
                    if (num == HeadSpr)
                    {
                        if (!sleaser.sprites[num].element.name.Contains("HeadA"))
                        {
                            sleaser.sprites[num].element.name = spr.name.Replace("HeadB", "HeadA");
                            sleaser.sprites[num].element.name = spr.name.Replace("HeadC", "HeadA");
                            sleaser.sprites[num].element.name = spr.name.Replace("HeadD", "HeadA");
                        }
                    }
                    if (num == FaceSpr)
                    {
                        if (sleaser.sprites[num].element.name.Contains("PFace"))
                        {
                            sleaser.sprites[num].element.name = spr.name.Replace("PFace", "Face");
                        }
                    }
                }
            }
        }

            //Make the tail loooooong
            void PlayerGraphics_Ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (self.player.slugcatStats.name == Nymph)
            {
                self.tail = new TailSegment[8];
                self.tail[0] = new TailSegment(self, 6f, 4f, null, 0.1f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 4f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 2f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 1f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
                self.tail[4] = new TailSegment(self, 0.5f, 7f, self.tail[3], 0.85f, 1f, 0.5f, true);
                self.tail[5] = new TailSegment(self, 0.3f, 7f, self.tail[4], 0.85f, 1f, 0.5f, true);
                self.tail[6] = new TailSegment(self, 0.25f, 7f, self.tail[5], 0.85f, 1f, 0.5f, true);
                self.tail[7] = new TailSegment(self, 0.2f, 7f, self.tail[6], 0.85f, 1f, 0.5f, true);
                var bp = self.bodyParts.ToList();
                bp.RemoveAll(x => x is TailSegment);
                bp.AddRange(self.tail);

                self.bodyParts = bp.ToArray();
            }
        }

        //Move the arms down a tad
        void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            if (self.player.slugcatStats.name == Nymph)
            {
                var data = Data(self.player);
                //Make the mark of communication invisible
                self.markAlpha = 0;
                self.lastMarkAlpha = 0;
                self.markBaseAlpha = 0;
                //Moving arms down 
                { 
                if (self.player.swallowAndRegurgitateCounter > 0 && (self.player.bodyMode == Player.BodyModeIndex.Stand || self.player.animation == Player.AnimationIndex.BeamTip))
                {
                    foreach (var hnd in self.hands)
                    {
                        hnd.pos.y = Mathf.Lerp(hnd.lastPos.y - 4f, hnd.pos.y - 4f, 1f);
                    }
                }
                else if (self.player.animation == Player.AnimationIndex.HangUnderVerticalBeam && self.player.bodyMode != Player.BodyModeIndex.Stand)
                {
                    foreach (var hnd in self.hands)
                    {
                        hnd.pos.y = Mathf.Lerp(hnd.lastPos.y - 8f, hnd.pos.y - 8f, 1f);
                    }
                    }

                }



            }
            orig(self);
        }

    }
}