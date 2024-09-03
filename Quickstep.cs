using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using ServerSync;
using System.Collections.Generic;
using System;

namespace Quickstep
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class Quickstep : BaseUnityPlugin
    {
        const string pluginID = "shudnal.Quickstep";
        const string pluginName = "Quickstep";
        const string pluginVersion = "1.0.6";

        private readonly Harmony harmony = new Harmony(pluginID);

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
        private static ConfigEntry<bool> allowCustomPrefabs;

        private static ConfigEntry<float> dashForceBareFists;
        private static ConfigEntry<float> dashTimeBareFists;
        private static ConfigEntry<float> dashForceSwords;
        private static ConfigEntry<float> dashTimeSwords;
        private static ConfigEntry<float> dashForceKnives;
        private static ConfigEntry<float> dashTimeKnives;
        private static ConfigEntry<float> dashForceClubs;
        private static ConfigEntry<float> dashTimeClubs;
        private static ConfigEntry<float> dashForcePolearms;
        private static ConfigEntry<float> dashTimePolearms;
        private static ConfigEntry<float> dashForceSpears;
        private static ConfigEntry<float> dashTimeSpears;
        private static ConfigEntry<float> dashForceAxes;
        private static ConfigEntry<float> dashTimeAxes;
        private static ConfigEntry<float> dashForceBows;
        private static ConfigEntry<float> dashTimeBows;
        private static ConfigEntry<float> dashForceElementalMagic;
        private static ConfigEntry<float> dashTimeElementalMagic;
        private static ConfigEntry<float> dashForceBloodMagic;
        private static ConfigEntry<float> dashTimeBloodMagic;
        private static ConfigEntry<float> dashForceUnarmed;
        private static ConfigEntry<float> dashTimeUnarmed;
        private static ConfigEntry<float> dashForcePickaxes;
        private static ConfigEntry<float> dashTimePickaxes;
        private static ConfigEntry<float> dashForceCrossbows;
        private static ConfigEntry<float> dashTimeCrossbows;
        private static ConfigEntry<float> dashForceCustomPrefabs;
        private static ConfigEntry<float> dashTimeCustomPrefabs;

        private static ConfigEntry<string> prefabListUseBareFistsConfig;
        private static ConfigEntry<string> prefabListUseSwordsConfig;
        private static ConfigEntry<string> prefabListUseKnivesConfig;
        private static ConfigEntry<string> prefabListUseClubsConfig;
        private static ConfigEntry<string> prefabListUsePolearmsConfig;
        private static ConfigEntry<string> prefabListUseSpearsConfig;
        private static ConfigEntry<string> prefabListUseAxesConfig;
        private static ConfigEntry<string> prefabListUseBowsConfig;
        private static ConfigEntry<string> prefabListUseElementalMagicConfig;
        private static ConfigEntry<string> prefabListUseBloodMagicConfig;
        private static ConfigEntry<string> prefabListUseUnarmedConfig;
        private static ConfigEntry<string> prefabListUsePickaxesConfig;
        private static ConfigEntry<string> prefabListUseCrossbowsConfig;
        private static ConfigEntry<string> prefabListUseCustomPrefabsConfig;

        internal static Quickstep instance;

        private static Dictionary<string, Tuple<ConfigEntry<float>, ConfigEntry<float>>> customPrefabs = new Dictionary<string, Tuple<ConfigEntry<float>, ConfigEntry<float>>>();

        private static bool isDashed;

        private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        private static readonly WaitForSeconds waitFor001Sec = new WaitForSeconds(0.01f);

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;
        }

        private void OnDestroy()
        {
            Config.Save();
            harmony?.UnpatchSelf();
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
            allowCustomPrefabs = config("Weapons", "Allow quickstep with custom weapon list", defaultValue: false, "Perform quickstep instead of dodging while using weapon from list");

            dashForceBareFists = config("Weapons - Details", "Dash force with bare fists", defaultValue: 0.0f, "Dash force while not using any weapon. Set 0 to use default value.");
            dashTimeBareFists = config("Weapons - Details", "Dash time with bare fists", defaultValue: 0.0f, "Dash time while not using any weapon. Set 0 to use default value.");
            dashForceSwords = config("Weapons - Details", "Dash force with Swords", defaultValue: 0.0f, "Dash force while using Swords. Set 0 to use default value.");
            dashTimeSwords = config("Weapons - Details", "Dash time with Swords", defaultValue: 0.0f, "Dash time while using Swords. Set 0 to use default value.");
            dashForceKnives = config("Weapons - Details", "Dash force with Knives", defaultValue: 0.0f, "Dash force while using Knives. Set 0 to use default value.");
            dashTimeKnives = config("Weapons - Details", "Dash time with Knives", defaultValue: 0.0f, "Dash time while using Knives. Set 0 to use default value.");
            dashForceClubs = config("Weapons - Details", "Dash force with Clubs", defaultValue: 0.0f, "Dash force while using Clubs. Set 0 to use default value.");
            dashTimeClubs = config("Weapons - Details", "Dash time with Clubs", defaultValue: 0.0f, "Dash time while using Clubs. Set 0 to use default value.");
            dashForcePolearms = config("Weapons - Details", "Dash force with Polearms", defaultValue: 0.0f, "Dash force while using Polearms. Set 0 to use default value.");
            dashTimePolearms = config("Weapons - Details", "Dash time with Polearms", defaultValue: 0.0f, "Dash time while using Polearms. Set 0 to use default value.");
            dashForceSpears = config("Weapons - Details", "Dash force with Spears", defaultValue: 0.0f, "Dash force while using Spears. Set 0 to use default value.");
            dashTimeSpears = config("Weapons - Details", "Dash time with Spears", defaultValue: 0.0f, "Dash time while using Spears. Set 0 to use default value.");
            dashForceAxes = config("Weapons - Details", "Dash force with Axes", defaultValue: 0.0f, "Dash force while using Axes. Set 0 to use default value.");
            dashTimeAxes = config("Weapons - Details", "Dash time with Axes", defaultValue: 0.0f, "Dash time while using Axes. Set 0 to use default value.");
            dashForceBows = config("Weapons - Details", "Dash force with Bows", defaultValue: 0.0f, "Dash force while using Bows. Set 0 to use default value.");
            dashTimeBows = config("Weapons - Details", "Dash time with Bows", defaultValue: 0.0f, "Dash time while using Bows. Set 0 to use default value.");
            dashForceElementalMagic = config("Weapons - Details", "Dash force with ElementalMagic", defaultValue: 0.0f, "Dash force while using ElementalMagic staff. Set 0 to use default value.");
            dashTimeElementalMagic = config("Weapons - Details", "Dash time with ElementalMagic", defaultValue: 0.0f, "Dash time while using ElementalMagic staff. Set 0 to use default value.");
            dashForceBloodMagic = config("Weapons - Details", "Dash force with BloodMagic", defaultValue: 0.0f, "Dash force while using BloodMagic staff. Set 0 to use default value.");
            dashTimeBloodMagic = config("Weapons - Details", "Dash time with BloodMagic", defaultValue: 0.0f, "Dash time while using BloodMagic staff. Set 0 to use default value.");
            dashForceUnarmed = config("Weapons - Details", "Dash force with Unarmed", defaultValue: 0.0f, "Dash force while using Unarmed. Set 0 to use default value.");
            dashTimeUnarmed = config("Weapons - Details", "Dash time with Unarmed", defaultValue: 0.0f, "Dash time while using Unarmed. Set 0 to use default value.");
            dashForcePickaxes = config("Weapons - Details", "Dash force with Pickaxes", defaultValue: 0.0f, "Dash force while using Pickaxes. Set 0 to use default value.");
            dashTimePickaxes = config("Weapons - Details", "Dash time with Pickaxes", defaultValue: 0.0f, "Dash time while using Pickaxes. Set 0 to use default value.");
            dashForceCrossbows = config("Weapons - Details", "Dash force with Crossbows", defaultValue: 0.0f, "Dash force while using Crossbows. Set 0 to use default value.");
            dashTimeCrossbows = config("Weapons - Details", "Dash time with Crossbows", defaultValue: 0.0f, "Dash time while using Crossbows. Set 0 to use default value.");
            dashForceCustomPrefabs = config("Weapons - Details", "Dash force with Custom prefabs", defaultValue: 0.0f, "Dash force while using prefabs from custom list. Set 0 to use default value.");
            dashTimeCustomPrefabs = config("Weapons - Details", "Dash time with Custom prefabs", defaultValue: 0.0f, "Dash time while using prefabs from custom list. Set 0 to use default value.");

            prefabListUseBareFistsConfig = config("Weapons - Prefabs", "Prefab list to use Bare fists config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from BareFists config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseSwordsConfig = config("Weapons - Prefabs", "Prefab list to use Swords config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Swords config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseKnivesConfig = config("Weapons - Prefabs", "Prefab list to use Knives config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Knives config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseClubsConfig = config("Weapons - Prefabs", "Prefab list to use Clubs config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Clubs config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUsePolearmsConfig = config("Weapons - Prefabs", "Prefab list to use Polearms config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Polearms config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseSpearsConfig = config("Weapons - Prefabs", "Prefab list to use Spears config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Spears config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseAxesConfig = config("Weapons - Prefabs", "Prefab list to use Axes config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Axes config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseBowsConfig = config("Weapons - Prefabs", "Prefab list to use Bows config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Bows config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseElementalMagicConfig = config("Weapons - Prefabs", "Prefab list to use ElementalMagic config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from ElementalMagic config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseBloodMagicConfig = config("Weapons - Prefabs", "Prefab list to use BloodMagic config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from BloodMagic config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseUnarmedConfig = config("Weapons - Prefabs", "Prefab list to use Unarmed config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Unarmed config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUsePickaxesConfig = config("Weapons - Prefabs", "Prefab list to use Pickaxes config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Pickaxes config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseCrossbowsConfig = config("Weapons - Prefabs", "Prefab list to use Crossbows config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from Crossbows config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));
            prefabListUseCustomPrefabsConfig = config("Weapons - Prefabs", "Prefab list to use Custom prefabs config", defaultValue: "",
                       new ConfigDescription("Comma-separated list of prefabs to use dash force and time from CustomPrefabs config", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings(",") }));

            prefabListUseBareFistsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseSwordsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseKnivesConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseClubsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUsePolearmsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseSpearsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseAxesConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseBowsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseElementalMagicConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseBloodMagicConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseUnarmedConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUsePickaxesConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseCrossbowsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
            prefabListUseCustomPrefabsConfig.SettingChanged += (sender, args) => UpdateCustomPrefabs();
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        private static void UpdateCustomPrefabs()
        {
            customPrefabs.Clear();

            AddToCustomConfigs(prefabListUseBareFistsConfig, dashForceBareFists, dashTimeBareFists);
            AddToCustomConfigs(prefabListUseSwordsConfig, dashForceSwords, dashTimeSwords);
            AddToCustomConfigs(prefabListUseKnivesConfig, dashForceKnives, dashTimeKnives);
            AddToCustomConfigs(prefabListUseClubsConfig, dashForceClubs, dashTimeClubs);
            AddToCustomConfigs(prefabListUsePolearmsConfig, dashForcePolearms, dashTimePolearms);
            AddToCustomConfigs(prefabListUseSpearsConfig, dashForceSpears, dashTimeSpears);
            AddToCustomConfigs(prefabListUseAxesConfig, dashForceAxes, dashTimeAxes);
            AddToCustomConfigs(prefabListUseBowsConfig, dashForceBows, dashTimeBows);
            AddToCustomConfigs(prefabListUseElementalMagicConfig, dashForceElementalMagic, dashTimeElementalMagic);
            AddToCustomConfigs(prefabListUseBloodMagicConfig, dashForceBloodMagic, dashTimeBloodMagic);
            AddToCustomConfigs(prefabListUseUnarmedConfig, dashForceUnarmed, dashTimeUnarmed);
            AddToCustomConfigs(prefabListUsePickaxesConfig, dashForcePickaxes, dashTimePickaxes);
            AddToCustomConfigs(prefabListUseCrossbowsConfig, dashForceCrossbows, dashTimeCrossbows);
            AddToCustomConfigs(prefabListUseCustomPrefabsConfig, dashForceCustomPrefabs, dashTimeCustomPrefabs);

            static void AddToCustomConfigs(ConfigEntry<string> customConfigs, ConfigEntry<float> dashForce, ConfigEntry<float> dashTime)
            {
                foreach (string prefabName in customConfigs.Value.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    customPrefabs.Add(prefabName, Tuple.Create(dashForce, dashTime));
            }
        }

        public static IEnumerator Dash(Player player, Vector3 dodgeDir, bool reducedIFrames, float dashForceWeapon, float dashTimeWeapon, Vector3 currentVel)
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
                yield return waitForFixedUpdate;
            }

            // The quickstep animation is basically nonfinished crouch animation
            player.SetCrouch(!isCrouching);
            player.m_zanim.SetBool(Player.s_crouching, !isCrouching);
            
            yield return waitForFixedUpdate;

            // Disable equipping animation as we already transitioned to crouch animation
            player.m_zanim.SetBool("equipping", false);

            yield return waitForFixedUpdate;

            // Make crouch animation faster for better effect
            player.m_zanim.SetSpeed(speed * 1.5f);

            float dashTimeCurrent = (dashTimeWeapon == 0.0f ? dashTime.Value : dashTimeWeapon);

            float m_time = Time.time + dashTimeCurrent;

            Vector3 vector = dodgeDir * (dashForceWeapon == 0.0f ? dashForce.Value : dashForceWeapon);
            vector.y = 0.0f;

            while (Time.time < m_time)
            {
                player.m_body.AddForce(vector, ForceMode.Impulse);

                bool invincibility = (!reducedIFrames) || (m_time - Time.time) >= (dashTimeCurrent - Mathf.Clamp(dashInvincibilityTime.Value, 0f, dashTimeCurrent));
                // If player have a shield than invincibility frames will be reduced
                if (player.m_dodgeInvincible && !invincibility)
                {
                    player.m_dodgeInvincible = invincibility;
                    player.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, player.m_dodgeInvincible);
                }
                
                yield return waitFor001Sec;
            }

            // Let body save some velocity after quickstep
            player.m_body.velocity = Vector3.Lerp(player.m_body.velocity, currentVel, 0.5f) * 0.3f;

            if (player.m_dodgeInvincible)
            {
                player.m_dodgeInvincible = false;
                player.m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, player.m_dodgeInvincible);
            }

            player.m_inDodge = false;

            yield return waitForFixedUpdate;

            // Return crouching state
            player.SetCrouch(isCrouching);
            player.m_zanim.SetBool(Player.s_crouching, isCrouching);

            // Return animation speed
            player.m_zanim.SetSpeed(speed);

            yield return new WaitForSeconds(dashCooldownTime.Value);

            isDashed = false;
        }

        public static bool AllowQuickstep(Player player, out float dashForceWeapon, out float dashTimeWeapon)
        {
            dashForceWeapon = 0f;
            dashTimeWeapon = 0f;

            if (player == null) 
                return false;

            ItemDrop.ItemData weapon = player.GetRightItem() ?? player.GetLeftItem();
            if (weapon != null)
            {
                if (weapon.m_dropPrefab != null && customPrefabs.TryGetValue(weapon.m_dropPrefab.name, out Tuple<ConfigEntry<float>, ConfigEntry<float>> tuple1))
                {
                    dashForceWeapon = tuple1.Item1.Value;
                    dashTimeWeapon = tuple1.Item2.Value;
                    return allowCustomPrefabs.Value;
                }

                if (customPrefabs.TryGetValue(weapon.m_shared.m_name, out Tuple<ConfigEntry<float>, ConfigEntry<float>> tuple2))
                {
                    dashForceWeapon = tuple2.Item1.Value;
                    dashTimeWeapon = tuple2.Item2.Value;
                    return allowCustomPrefabs.Value;
                }
            }

            if (weapon == null || weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                dashForceWeapon = dashForceBareFists.Value;
                dashTimeWeapon = dashTimeBareFists.Value;
                return allowBareFists.Value;
            }
                
            Skills.SkillType skill = weapon.m_shared.m_skillType;

            switch (skill)
            {
                case Skills.SkillType.Swords:
                    dashForceWeapon = dashForceSwords.Value;
                    dashTimeWeapon = dashTimeSwords.Value;
                    return allowSwords.Value;
                case Skills.SkillType.Knives:
                    dashForceWeapon = dashForceKnives.Value;
                    dashTimeWeapon = dashTimeKnives.Value;
                    return allowKnives.Value;
                case Skills.SkillType.Clubs:
                    dashForceWeapon = dashForceClubs.Value;
                    dashTimeWeapon = dashTimeClubs.Value;
                    return allowClubs.Value;
                case Skills.SkillType.Polearms:
                    dashForceWeapon = dashForcePolearms.Value;
                    dashTimeWeapon = dashTimePolearms.Value;
                    return allowPolearms.Value;
                case Skills.SkillType.Spears:
                    dashForceWeapon = dashForceSpears.Value;
                    dashTimeWeapon = dashTimeSpears.Value;
                    return allowSpears.Value;
                case Skills.SkillType.Axes:
                    dashForceWeapon = dashForceAxes.Value;
                    dashTimeWeapon = dashTimeAxes.Value;
                    return allowAxes.Value;
                case Skills.SkillType.Bows:
                    dashForceWeapon = dashForceBows.Value;
                    dashTimeWeapon = dashTimeBows.Value;
                    return allowBows.Value;
                case Skills.SkillType.ElementalMagic:
                    dashForceWeapon = dashForceElementalMagic.Value;
                    dashTimeWeapon = dashTimeElementalMagic.Value;
                    return allowElementalMagic.Value;
                case Skills.SkillType.BloodMagic:
                    dashForceWeapon = dashForceBloodMagic.Value;
                    dashTimeWeapon = dashTimeBloodMagic.Value;
                    return allowBloodMagic.Value;
                case Skills.SkillType.Unarmed:
                    dashForceWeapon = dashForceUnarmed.Value;
                    dashTimeWeapon = dashTimeUnarmed.Value;
                    return allowUnarmed.Value;
                case Skills.SkillType.Pickaxes:
                    dashForceWeapon = dashForcePickaxes.Value;
                    dashTimeWeapon = dashTimePickaxes.Value;
                    return allowPickaxes.Value;
                case Skills.SkillType.Crossbows:
                    dashForceWeapon = dashForceCrossbows.Value;
                    dashTimeWeapon = dashTimeCrossbows.Value;
                    return allowCrossbows.Value;
            }

            return false;
        }

        public static void PerformQuickstep(Player __instance, float dt, ref float ___m_queuedDodgeTimer, Vector3 ___m_queuedDodgeDir, float stam, bool reducedIFrames, float dashForceWeapon, float dashTimeWeapon, Vector3 currentVel)
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

                    // We deal less noise than dodge(5f)
                    __instance.AddNoise(3f);
                    __instance.UseStamina(stam);
                    __instance.UpdateBodyFriction();
                    __instance.m_dodgeEffects.Create(__instance.transform.position, Quaternion.identity, __instance.transform);

                    __instance.StartCoroutine(Dash(__instance, ___m_queuedDodgeDir, reducedIFrames, dashForceWeapon, dashTimeWeapon, currentVel));
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
            private static bool Prefix(Player __instance, float dt, ref float ___m_queuedDodgeTimer)
            {
                if (!modEnabled.Value)
                    return true;

                if (!AllowQuickstep(__instance, out float dashForceWeapon, out float dashTimeWeapon))
                    return true;

                // Quickstep use less stamina that dodge
                float staminaUse = __instance.m_dodgeStaminaUsage;
                staminaUse = staminaUse - staminaUse * __instance.GetEquipmentMovementModifier() + staminaUse * __instance.GetEquipmentDodgeStaminaModifier();
                staminaUse *= dashStaminaMultiplier.Value;

                // Equipped shield reduces ability to perform a dash with full invincibility 
                bool reducedIFrames = (__instance.GetLeftItem() != null) && __instance.GetLeftItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;

                PerformQuickstep(__instance, dt, ref ___m_queuedDodgeTimer, __instance.m_queuedDodgeDir, staminaUse, reducedIFrames, dashForceWeapon, dashTimeWeapon, __instance.m_currentVel);

                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSneaking))]
        public static class Player_OnSneaking_PreventSneakXP
        {
            private static void Prefix(Player __instance, ref float dt)
            {
                if (!modEnabled.Value) 
                    return;

                if (isDashed) 
                    dt = 0;
            }
        }

    }

}
