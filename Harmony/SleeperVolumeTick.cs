﻿using HarmonyLib;
using SpawnSleepersInRange.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SpawnSleepersInRange.Harmony
{
    [HarmonyPatch(typeof(global::SleeperVolume))]
    [HarmonyPatch("Tick")]
    public class SleeperVolumeTick
    {
        private static MethodInfo touchGroup;
        private static FieldInfo hasPassives;

        public static void Postfix(SleeperVolume __instance, World _world)
        {
            if (__instance == null || _world == null)
            {
                return;
            }

            if (hasPassives == null)
            {
                hasPassives = typeof(SleeperVolume).GetField("hasPassives", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (!__instance.wasCleared && !(bool)hasPassives.GetValue(__instance))
            {
                if (touchGroup == null)
                {
                    touchGroup = typeof(SleeperVolume).GetMethod("TouchGroup", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                PrefabInstance POI = _world.GetPOIAtPosition(__instance.Center);

                foreach (EntityPlayer player in _world.Players.list)
                {                   
                    if (Config.Instance.OnlySpawnInCurrentPOI || player.AttachedToEntity is EntityVehicle)
                    {
                        if (POI == null || POI != _world.GetPOIAtPosition(player.position))
                        {
                            continue;
                        }
                    }                   

                    if (PlayerWithinRange(__instance, player))
                    {
                        // SleeperVolume.TouchGroup() handles *most* spawns, except for special triggers
                        touchGroup.Invoke(__instance, new object[] { _world, player, false });
                        break;
                    }
                }
            }
        }

        private static bool PlayerWithinRange(SleeperVolume volume, EntityPlayer player)
        {
            // for smaller spawn radius ranges we really need to check more than the volume center, because volumes can easily be 2-3x
            // larger than the radius, which would end up being the very same pop-in spawn issue we see with triggers.
            // in such cases, we want to also check against the box corners
            if (Config.Instance.SpawnRadius <= 15.0f)
            {
                List<Vector3> points = new List<Vector3> { volume.Center };
                points.AddRange(CalculateBoxCorners(volume.BoxMax, volume.BoxMin));

                foreach (Vector3 point in points)
                {
                    if (CheckDistance(point, player))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return CheckDistance(volume.Center, player);
            }
        }

        private static bool CheckDistance(Vector3 point, EntityPlayer player)
        {
            if (Config.Instance.UseSplitSpawnRadii)
            {
                if (Math.Abs(point.y - player.position.y) <= Config.Instance.VerticalSpawnRadius)
                {
                    if (Vector2.Distance(point, player.position) <= Config.Instance.SpawnRadius)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return Vector3.Distance(point, player.position) <= Config.Instance.SpawnRadius;
            }
        }

        private static Vector3[] CalculateBoxCorners(Vector3 boxMax, Vector3 boxMin)
        {
            var corners = new Vector3[8];

            corners[0] = new Vector3(boxMax.x, boxMax.y, boxMax.z);
            corners[1] = new Vector3(boxMax.x, boxMax.y, boxMin.z);
            corners[2] = new Vector3(boxMax.x, boxMin.y, boxMax.z);
            corners[3] = new Vector3(boxMax.x, boxMin.y, boxMin.z);
            corners[4] = new Vector3(boxMin.x, boxMax.y, boxMax.z);
            corners[5] = new Vector3(boxMin.x, boxMax.y, boxMin.z);
            corners[6] = new Vector3(boxMin.x, boxMin.y, boxMax.z);
            corners[7] = new Vector3(boxMin.x, boxMin.y, boxMin.z);

            return corners;
        }
    }
}
