using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using ServerSync;

namespace Quickstep
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class Quickstep : BaseUnityPlugin
    {
        const string pluginID = "shudnal.Quickstep";
        const string pluginName = "Quickstep";
        const string pluginVersion = "1.0.0";

        private Harmony _harmony;

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> configLocked;
        private static ConfigEntry<bool> loggingEnabled;

        private static ConfigEntry<float> dashForce;
        private static ConfigEntry<float> dashTime;
        private static ConfigEntry<float> dashInvincibilityTime;
        private static ConfigEntry<float> dashCooldownTime;
        private static ConfigEntry<float> dashStaminaMultiplier;

        private static ConfigEntry<bool> allowBareFists;

        private static ConfigEntry<bool> allowSwords;
        private static ConfigEntry<bool> allowKnives;
        private static ConfigEntry<bool> allowClubs;
        private static ConfigEntry<bool> allowPolearms;
        private static ConfigEntry<bool> allowSpears;
        private static ConfigEntry<bool> allowAxes;
        private static ConfigEntry<bool> allowBows;
        private static ConfigEntry<bool> allowElementalMagic;
        private static ConfigEntry<bool> allowBloodMagic;
        private static ConfigEntry<bool> allowUnarmed;
        private static ConfigEntry<bool> allowPickaxes;
        private static ConfigEntry<bool> allowCrossbows;

        internal static Quickstep instance;

        private static bool isDashed;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), pluginID);

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);
        }

        private void OnDestroy()
        {
            Config.Save();
            _harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        private void ConfigInit()
        {
            config("General", "NexusID", 2547, "Nexus mod ID for updates");

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable the mod");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging for debug events. [Not Synced with Server]", false);

            dashForce = config("Quickstep", "Dash force", defaultValue: 50.0f, "Quickstep force factor.");
            dashTime = config("Quickstep", "Dash time", defaultValue: 0.25f, "Quickstep time.");
            dashInvincibilityTime = config("Quickstep", "Invincibility time with shield", defaultValue: 0.15f, "Quickstep invincibility time when you're equipping a shield. If you're not equipping a shield quickstep grant invincibility for all its duration.");
            dashCooldownTime = config("Quickstep", "Quickstep cooldown", defaultValue: 0.5f, "Time in seconds you can not dash again.");
            dashStaminaMultiplier = config("Quickstep", "Stamina usage multiplier", defaultValue: 0.6f, "Multiplier of how much less stamina you will use on quickstep than dodge stamina usage.");

            allowBareFists = config("Weapons", "Allow quickstep with bare fists", defaultValue: false, "Perform quickstep instead of dodging while not using any weapon");
            allowSwords = config("Weapons", "Allow quickstep with Swords", defaultValue: false, "Perform quickstep instead of dodging while using Swords");
            allowKnives = config("Weapons", "Allow quickstep with Knives", defaultValue: true, "Perform quickstep instead of dodging while using Knives");
            allowClubs = config("Weapons", "Allow quickstep with Clubs", defaultValue: false, "Perform quickstep instead of dodging while using Clubs");
            allowPolearms = config("Weapons", "Allow quickstep with Polearms", defaultValue: false, "Perform quickstep instead of dodging while using Polearms");
            allowSpears = config("Weapons", "Allow quickstep with Spears", defaultValue: false, "Perform quickstep instead of dodging while using Spears");
            allowAxes = config("Weapons", "Allow quickstep with Axes", defaultValue: false, "Perform quickstep instead of dodging while using Axes");
            allowBows = config("Weapons", "Allow quickstep with Bows", defaultValue: false, "Perform quickstep instead of dodging while using Bows");
            allowElementalMagic = config("Weapons", "Allow quickstep with ElementalMagic", defaultValue: false, "Perform quickstep instead of dodging while using ElementalMagic staff");
            allowBloodMagic = config("Weapons", "Allow quickstep with BloodMagic", defaultValue: false, "Perform quickstep instead of dodging while using BloodMagic staff");
            allowUnarmed = config("Weapons", "Allow quickstep with Unarmed", defaultValue: true, "Perform quickstep instead of dodging while using Unarmed");
            allowPickaxes = config("Weapons", "Allow quickstep with Pickaxes", defaultValue: false, "Perform quickstep instead of dodging while using Pickaxes");
            allowCrossbows = config("Weapons", "Allow quickstep with Crossbows", defaultValue: false, "Perform quickstep instead of dodging while using Crossbows");
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        public static IEnumerator Dash(Player player, Vector3 dodgeDir, bool reducedIFrames)
        {
            isDashed = true;
            bool isCrouching = player.IsCrouching();
            bool isBlocking = player.IsBlocking();

            player.ClearActionQueue();

            // Initiate dodging state to prevent damage
            player.m_inDodge = true;
            player.m_dodgeInvincible = true;
            player.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, player.m_dodgeInvincible);

            float speed = player.m_zanim.m_animator.speed;
            if (!isCrouching)
            {
                // Set speed of transition to crouch animations
                player.m_zanim.SetSpeed(speed * 3);
            }

            if (isBlocking)
            {
                // Disable blocking state to not registering hits while dodging
                player.m_internalBlockingState = !isBlocking;
                player.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, value: !isBlocking);
                player.m_zanim.SetBool(Player.s_blocking, value: !isBlocking);
                yield return new WaitForFixedUpdate();
            }

            // The quickstep animation is basically nonfinished crouch animation
            player.SetCrouch(!isCrouching);
            player.m_zanim.SetBool(Player.s_crouching, !isCrouching);
            
            yield return new WaitForFixedUpdate();

            // Disable equipping animation as we already transitioned to crouch animation
            player.m_zanim.SetBool("equipping", false);

            yield return new WaitForFixedUpdate();

            // Make crouch animation faster for better effect
            player.m_zanim.SetSpeed(speed * 1.5f);

            float m_time = Time.time + dashTime.Value;

            Vector3 vector = dodgeDir * dashForce.Value;
            vector.y = 0.0f;

            while (Time.time < m_time)
            {
                player.m_body.AddForce(vector, ForceMode.Impulse);

                bool invincibility = (!reducedIFrames) || (m_time - Time.time) >= (dashTime.Value - Mathf.Clamp(dashInvincibilityTime.Value, 0f, dashTime.Value));
                // If player have a shield than invincibility frames will be reduced
                if (player.m_dodgeInvincible && !invincibility)
                {
                    player.m_dodgeInvincible = invincibility;
                    player.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, player.m_dodgeInvincible);
                }
                
                yield return new WaitForSeconds(0.01f);
            }

            // Let body save some velocity after quickstep
            player.m_body.velocity = Vector3.Lerp(player.m_body.velocity, Vector3.zero, 0.2f);

            if (player.m_dodgeInvincible)
            {
                player.m_dodgeInvincible = false;
                player.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, player.m_dodgeInvincible);
            }

            player.m_inDodge = false;

            yield return new WaitForFixedUpdate();

            // Return crouching state
            player.SetCrouch(isCrouching);
            player.m_zanim.SetBool(Player.s_crouching, isCrouching);

            // Return animation speed
            player.m_zanim.SetSpeed(speed);

            yield return new WaitForSeconds(dashCooldownTime.Value);

            isDashed = false;
        }

        public static bool AllowQuickstep(Player player)
        {
            if (player == null) 
                return false;

            ItemDrop.ItemData weapon = player.GetRightItem();
            if (weapon == null)
                weapon = player.GetLeftItem();

            if (weapon == null || weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                return allowBareFists.Value;

            Skills.SkillType skill = weapon.m_shared.m_skillType;

            return 
                (allowSwords.Value && skill == Skills.SkillType.Swords) ||
                (allowKnives.Value && skill == Skills.SkillType.Knives) ||
                (allowClubs.Value && skill == Skills.SkillType.Clubs) ||
                (allowPolearms.Value && skill == Skills.SkillType.Polearms) ||
                (allowSpears.Value && skill == Skills.SkillType.Spears) ||
                (allowAxes.Value && skill == Skills.SkillType.Axes) ||
                (allowBows.Value && skill == Skills.SkillType.Bows) ||
                (allowElementalMagic.Value && skill == Skills.SkillType.ElementalMagic) ||
                (allowBloodMagic.Value && skill == Skills.SkillType.BloodMagic) ||
                (allowUnarmed.Value && skill == Skills.SkillType.Unarmed) ||
                (allowPickaxes.Value && skill == Skills.SkillType.Pickaxes) ||
                (allowCrossbows.Value && skill == Skills.SkillType.Crossbows);
        }

        public static void PerformQuickstep(Player __instance, float dt, ref float ___m_queuedDodgeTimer, Vector3 ___m_queuedDodgeDir, float stam, bool reducedIFrames)
        {
            ___m_queuedDodgeTimer -= dt;
            if (___m_queuedDodgeTimer > 0f && __instance.IsOnGround() && !__instance.IsDead() && !__instance.InAttack() && !__instance.IsEncumbered() && !__instance.InDodge() && !__instance.IsStaggering() && !isDashed)
            {
                if (__instance.HaveStamina(stam))
                {
                    ___m_queuedDodgeTimer = 0f;

                    // If player is not crouching then Crouching animation does need a workaround
                    // The Blocking animation does not allow transition to Crouching animation which is an essential visual part of quickstep
                    // So a fast transition to the Equipping animation allows us to deal with it
                    if (!__instance.IsCrouching())
                        __instance.m_zanim.SetBool("equipping", true);

                    __instance.StartCoroutine(Dash(__instance, ___m_queuedDodgeDir, reducedIFrames));

                    // We deal less noise than dodge(5f)
                    __instance.AddNoise(3f);
                    __instance.UseStamina(stam);
                    __instance.UpdateBodyFriction();
                    __instance.m_dodgeEffects.Create(__instance.transform.position, Quaternion.identity, __instance.transform);
                }
                else
                {
                    Hud.instance.StaminaBarEmptyFlash();
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        public static class Player_UpdateDodge_Quickstep
        {
            private static bool Prefix(Player __instance, float dt, ref float ___m_queuedDodgeTimer, float ___m_dodgeStaminaUsage, float ___m_equipmentMovementModifier, Vector3 ___m_queuedDodgeDir)
            {
                if (!modEnabled.Value) return true;

                if (!AllowQuickstep(__instance)) return true;

                // Quickstep use less stamina that dodge
                float stam = (___m_dodgeStaminaUsage - ___m_dodgeStaminaUsage * ___m_equipmentMovementModifier) * dashStaminaMultiplier.Value;

                // Equipped shield reduces ability to perform a dash with full invincibility 
                bool reducedIFrames = (__instance.GetLeftItem() != null) && __instance.GetLeftItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;

                PerformQuickstep(__instance, dt, ref ___m_queuedDodgeTimer, ___m_queuedDodgeDir, stam, reducedIFrames);

                return false;

            }
        }

    }

}
