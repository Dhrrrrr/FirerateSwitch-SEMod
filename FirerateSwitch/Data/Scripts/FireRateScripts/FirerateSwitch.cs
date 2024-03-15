using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using SpaceEngineers.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.World;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Sandbox.Game.Weapons;


namespace Dhr.HEAmmo
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class FirerateSwitch : MySessionComponentBase
    {
        // Dictionarys for storing relevant data
        Dictionary<long, PlayerWeaponData> playerWeaponDic = new Dictionary<long, PlayerWeaponData>();
        Dictionary<string, WeaponData> weaponInfoDic = new Dictionary<string, WeaponData>();


        public override void BeforeStart()
        {
            weaponInfoDic.Add("UltimateAutomaticRifleItem", new WeaponData(new int[3] {3, 0, 1 }));
            weaponInfoDic.Add("AssaultRifleStandardItem", new WeaponData(new int[2] { 0, 1 }));
            weaponInfoDic.Add("AutomaticCarbineItem", new WeaponData(new int[2] { 0, 1 }));
            weaponInfoDic.Add("AutomaticRifleItem", new WeaponData(new int[2] { 0, 1 }));
        }


        public override void UpdateBeforeSimulation()
        {
            // Get input
            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.LeftAlt))
            {
                // Get tool
                IMyCharacter character = MyAPIGateway.Session.Player.Character;
                var characterTool = character.EquippedTool as IMyHandheldGunObject<MyGunBase>;

                // Check if tool exists
                if (characterTool != null && characterTool.PhysicalObject != null)
                {
                    WeaponData firerateInfo;

                    // Check if tool is one that can be switched
                    if (weaponInfoDic.TryGetValue(characterTool.PhysicalObject.SubtypeName, out firerateInfo))
                    {
                        PlayerWeaponData playerWeaponData;
                        int weaponState;

                        // Check if player data already exists for the weapon, if not create it
                        if (playerWeaponDic.TryGetValue(character.EntityId, out playerWeaponData))
                        {
                            // Switch the state of the weapon
                            playerWeaponData.SwitchState(characterTool.PhysicalObject.SubtypeName, firerateInfo);

                            weaponState = playerWeaponData.GetWeaponValue(characterTool.PhysicalObject.SubtypeName);
                        }
                        else
                        {
                            // Create new entry and switch its state
                            playerWeaponData = new PlayerWeaponData();

                            playerWeaponDic.Add(character.EntityId, playerWeaponData);
                            playerWeaponData.SwitchState(characterTool.PhysicalObject.SubtypeName, firerateInfo);

                            weaponState = playerWeaponData.GetWeaponValue(characterTool.PhysicalObject.SubtypeName);
                        }

                        // Get weapon defintion
                        var wepDef = characterTool.GunBase.WeaponDefinition;

                        // Get fire rate
                        int fireRate = weaponInfoDic[characterTool.PhysicalObject.SubtypeName].ShotAmountArray[weaponState];

                        if (wepDef != null)
                        {
                            // Get data and switch the fire rate
                            var AmmoData = wepDef.WeaponAmmoDatas[0];
                            AmmoData.ShotsInBurst = fireRate;

                            // Send notification for fire mod
                            switch (fireRate)
                            {
                                case 0:
                                    MyAPIGateway.Utilities.ShowNotification("Automatic", 1000);
                                    break;
                                case 1:
                                    MyAPIGateway.Utilities.ShowNotification("Semi-Automatic", 1000);
                                    break;
                                default:
                                    MyAPIGateway.Utilities.ShowNotification("Burst Fire", 1000);
                                    break;
                            }
                        }                   
                    }
                }
            }
        }

        /// <summary>
        /// Holds the data for a players weapon states
        /// </summary>
        public class PlayerWeaponData
        {
            // Weapon info for the player
            private Dictionary<string, int> weaponSaveState = new Dictionary<string, int>();

            /// <summary>
            /// Switches the state of the player weapons
            /// </summary>
            /// <param name="weaponID">ID of the weapon to change</param>
            public void SwitchState(string weaponID, WeaponData weaponsData)
            {
                int state; 

                // Check if weapon exists
                if (weaponSaveState.TryGetValue(weaponID, out state))
                {
                    // Check if its at the end of the array, if so then go back to start, else go up a tier
                    if (state == (weaponsData.FiringModesCount - 1))
                    {
                        weaponSaveState[weaponID] = 0;
                    }
                    else
                    {
                        weaponSaveState[weaponID] += 1;
                    }
                }
                else
                {
                    AddWeapon(weaponID);
                    SwitchState(weaponID, weaponsData);
                }
            }

            /// <summary>
            /// Add a new weapon to the players registry
            /// </summary>
            /// <param name="weaponID">ID of the weapon</param>
            public void AddWeapon(string weaponID)
            {
                int val;

                // Check ID doesn't already exist
                if (!weaponSaveState.TryGetValue(weaponID, out val))
                {
                    // Add new ID
                    weaponSaveState.Add(weaponID, 0);
                }
            }

            /// <summary>
            /// Get the value of the weapon, if none is found then standard return is false
            /// </summary>
            /// <param name="weaponID">ID of the weapon</param>
            /// <returns></returns>
            public int GetWeaponValue(string weaponID)
            {
                int state;

                // Check if value exists
                if (weaponSaveState.TryGetValue(weaponID, out state))
                {
                    return state;
                }
                else
                {
                    // Create a weapon and return 0 as standard mode
                    AddWeapon(weaponID);

                    return 0;
                }
            }
        }

        /// <summary>
        /// Holds the fire data for the weapon
        /// </summary>
        public class WeaponData
        {
            private int firingModes;
            private int[] shotsPer;

            public int FiringModesCount { get { return firingModes; } }
            public int[] ShotAmountArray { get { return shotsPer; } }

            /// <summary>
            /// Create weapon data
            /// </summary>
            /// <param name="fireInfo">Array with different firing modes, 0 will equal automatic</param>
            public WeaponData(int[] fireInfo)
            {
                shotsPer = fireInfo;
                firingModes = fireInfo.Length;
            }
        }
    }
}
