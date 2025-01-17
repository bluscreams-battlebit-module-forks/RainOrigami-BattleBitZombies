﻿using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BattleBitBaseModules;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Zombies;

[RequireModule(typeof(CommandHandler))]
[RequireModule(typeof(RichText))]
public class Zombies : BattleBitModule {
    #region STATIC SETTINGS
    internal const Team HUMANS = Team.TeamA;
    internal const Team ZOMBIES = Team.TeamB;

    private const SpawningRule ZOMBIES_SPAWN_RULE = SpawningRule.Flags | SpawningRule.SquadCaptain | SpawningRule.SquadMates | SpawningRule.Boats;
    private const SpawningRule HUMANS_SPAWN_RULE = SpawningRule.Flags | SpawningRule.RallyPoints | SpawningRule.SquadCaptain | SpawningRule.SquadMates;
    private const VehicleType ZOMBIES_VEHICLE_RULE = VehicleType.SeaVehicle;
    private const VehicleType HUMANS_VEHICLE_RULE = VehicleType.None;

    private static readonly string[] HUMAN_UNIFORM = new[] { "ANY_NU_Uniform_Survivor_00", "ANY_NU_Uniform_Survivor_01", "ANY_NU_Uniform_Survivor_02", "ANY_NU_Uniform_Survivor_03", "ANY_NU_Uniform_Survivor_04" };
    private static readonly string[] HUMAN_HELMET = new[] { "ANV2_Survivor_All_Helmet_00_A_Z", "ANV2_Survivor_All_Helmet_00_B_Z", "ANV2_Survivor_All_Helmet_01_A_Z", "ANV2_Survivor_All_Helmet_02_A_Z", "ANV2_Survivor_All_Helmet_03_A_Z", "ANV2_Survivor_All_Helmet_04_A_Z", "ANV2_Survivor_All_Helmet_05_A_Z", "ANV2_Survivor_All_Helmet_05_B_Z" };
    private static readonly string[] HUMAN_BACKPACK = new[] { "ANV2_Survivor_All_Backpack_00_A_H", "ANV2_Survivor_All_Backpack_00_A_N", "ANV2_Survivor_All_Backpack_01_A_H", "ANV2_Survivor_All_Backpack_01_A_N", "ANV2_Survivor_All_Backpack_02_A_N" };
    private static readonly string[] HUMAN_ARMOR = new[] { "ANV2_Survivor_All_Armor_00_A_L", "ANV2_Survivor_All_Armor_00_A_N", "ANV2_Survivor_All_Armor_01_A_L", "ANV2_Survivor_All_Armor_02_A_L" };
    private static readonly string[] ZOMBIE_EYES = new[] { "Eye_Zombie_01" };
    private static readonly string[] ZOMBIE_FACE = new[] { "Face_Zombie_01" };
    private static readonly string[] ZOMBIE_HAIR = new[] { "Hair_Zombie_01" };
    private static readonly string[] ZOMBIE_BODY = new[] { "Zombie_01" };
    private static readonly string[] ZOMBIE_UNIFORM = new[] { "ANY_NU_Uniform_Zombie_01" };
    private static readonly string[] ZOMBIE_HELMET = new[] { "ANV2_Universal_Zombie_Helmet_00_A_Z" };
    private static readonly string[] ZOMBIE_ARMOR = new[] { "ANV2_Universal_All_Armor_Null" };
    private static readonly string[] ZOMBIE_BACKPACK = new[] { "ANV2_Universal_All_Backpack_Null" };
    private static readonly string[] ZOMBIE_BELT = new[] { "ANV2_Universal_All_Belt_Null" };

    private static readonly string[] HUMAN_FLASHLIGHTS = new[] { Attachments.Flashlight.Name, Attachments.Searchlight.Name, Attachments.TacticalFlashlight.Name };

    private static readonly ZombieClass[] zombieClasses = new[]
    {
        new ZombieClass("Tank", 5, p =>
        {
            p.Modifications.ReceiveDamageMultiplier = 0.10f;
            p.Modifications.RunningSpeedMultiplier = 0.65f;
        }),
        //new ZombieClass("Boomer", 5, p =>
        //{
        //    p.SetLightGadget(Gadgets.C4.Name, 1);
        //}),
        new ZombieClass("Creeper", 5, p =>
        {
            p.SetLightGadget(Gadgets.SuicideC4.Name, 1);
        }),
        new ZombieClass("Flasher", 10, p =>
        {
            p.SetThrowable(Gadgets.Flashbang.Name, 4);
        }),
        new ZombieClass("Hunter", 10, p =>
        {
            p.Modifications.RunningSpeedMultiplier = 2.0f;
        }),
        new ZombieClass("Jumper", 20, p =>
        {
            p.Modifications.JumpHeightMultiplier = 3f;
        }),
        new ZombieClass("Climber", 20, p =>
        {
            p.SetLightGadget(Gadgets.GrapplingHook.Name, 1);
        }),
        new ZombieClass("Shielded", 30, p =>
        {
            p.SetHeavyGadget(Gadgets.RiotShield.Name, 1);
        }),
        new ZombieClass("Smoker", 5, p =>
        {
            p.SetLightGadget(Gadgets.M320SmokeGrenadeLauncher.Name, 6);
        })
    };

    private static readonly string[] allowedZombieMeleeGadgets = new[]
    {
        Gadgets.SledgeHammer.Name,
        Gadgets.SledgeHammerSkinA.Name,
        Gadgets.SledgeHammerSkinB.Name,
        Gadgets.SledgeHammerSkinC.Name,
        Gadgets.Pickaxe.Name,
        Gadgets.PickaxeIronPickaxe.Name
    };

    private static readonly string[] allowedZombieThrowables = new[]
    {
        Gadgets.SmokeGrenadeBlue.Name,
        Gadgets.SmokeGrenadeGreen.Name,
        Gadgets.SmokeGrenadeRed.Name,
        Gadgets.SmokeGrenadeWhite.Name
    };

    private static readonly WeaponItem emptyWeapon = new() {
        Barrel = null,
        BoltAction = null,
        CantedSight = null,
        MainSight = null,
        SideRail = null,
        Tool = null,
        TopSight = null,
        UnderRail = null
    };
    #endregion

    #region CONFIGURATION
    public ZombiesConfiguration Configuration { get; set; }
    public ZombiesState State { get; set; }
    public ZombiePersistenceStorage LoadoutStorage { get; set; }
    #endregion

    #region MODULES
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    [ModuleReference]
    public dynamic? DiscordWebhooks { get; set; }

    [ModuleReference]
    public RichText RichText { get; set; }
    #endregion

    #region ZOMBIE PLAYERS
    private Dictionary<ulong, ZombiesPlayer> players = new();

    private ZombiesPlayer getPlayer(RunnerPlayer player) {
        if (!this.players.ContainsKey(player.SteamID)) {
            this.players.Add(player.SteamID, new ZombiesPlayer(player, this.LoadoutStorage.Persistence.ContainsKey(player.SteamID) ? this.LoadoutStorage.Persistence[player.SteamID] : new ZombiePersistence()));
        }

        return this.players[player.SteamID];
    }
    #endregion

    #region GAME STATE MANAGEMENT
    private async Task gameStateManagerWorker() {
        Stopwatch stopwatch = new Stopwatch();
        while (this.IsLoaded && this.Server.IsConnected) {
            stopwatch.Restart();
            await manageGameState();
            this.State.Save();
            this.LoadoutStorage.Persistence = this.players.ToDictionary(p => p.Key, p => p.Value.Persistence!);
            try {
                this.LoadoutStorage.Save();
            } catch (Exception ex) {
                debugLog(ex.ToString());
            }
            stopwatch.Stop();
            int timeToWait = this.Configuration.GameStateUpdateTimer - (int)stopwatch.ElapsedMilliseconds;
            if (timeToWait > 0) {
                await Task.Delay(timeToWait);
            } else {
                Console.ForegroundColor = ConsoleColor.Yellow;
                await Console.Out.WriteLineAsync($"GameStateManager is running behind by {timeToWait}ms");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Handles changing of the game state
    /// </summary>
    /// <returns></returns>
    private async Task manageGameState() {
        ZombiesGameState oldState = this.State.GameState;

        // Transition to waiting for players, can transition from any state

        if (this.Server.RoundSettings.State == GameState.WaitingForPlayers && this.State.GameState != ZombiesGameState.WaitingForPlayers) {
            this.State.GameState = ZombiesGameState.WaitingForPlayers;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // Transition to countdown, can transition from any state

        if (this.Server.RoundSettings.State == GameState.CountingDown && this.State.GameState != ZombiesGameState.Countdown) {
            this.State.GameState = ZombiesGameState.Countdown;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // Transition to build phase, can only transition from countdown

        if (this.Server.RoundSettings.State == GameState.Playing && this.State.GameState == ZombiesGameState.Countdown) {
            this.State.GameState = ZombiesGameState.BuildPhase;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // Transition to playing, can transition from any state

        if (this.Server.RoundSettings.State == GameState.Playing && this.State.GameState == ZombiesGameState.BuildPhase && !this.State.BuildPhase) {
            this.State.GameState = ZombiesGameState.GamePhase;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // Transition to zombie win

        if (this.Server.RoundSettings.State == GameState.Playing && this.State.GameState == ZombiesGameState.GamePhase && isZombieWin()) {
            this.State.GameState = ZombiesGameState.ZombieWin;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // Transition to human win

        if (this.Server.RoundSettings.State == GameState.Playing && this.State.GameState == ZombiesGameState.GamePhase && isHumanWin()) {
            this.State.GameState = ZombiesGameState.HumanWin;
            await this.zombieGameStateChanged(oldState);
            return;
        }

        // No transition, tick the current state
        this.zombieGameStateTick();
    }

    int tick = 0;

    private void zombieGameStateTick() {
        tick++;
        if (tick % 40 == 0) {
            tick = 0;
            Console.WriteLine($"There are {this.Server.AllPlayers.Count()} players on the server ({this.Server.AllPlayers.Count(p => p.Team == ZOMBIES)} zombies, {this.Server.AllPlayers.Count(p => p.Team == HUMANS)} humans)");
        }

        switch (this.State.GameState) {
            case ZombiesGameState.WaitingForPlayers:
                this.waitingForPlayersGameStateTick();
                break;
            case ZombiesGameState.Countdown:
                this.countdownGameStateTick();
                break;
            case ZombiesGameState.BuildPhase:
                this.buildPhaseGameStateTick();
                break;
            case ZombiesGameState.GamePhase:
                this.gamePhaseGameStateTick();
                break;
            case ZombiesGameState.ZombieWin:
                this.zombieWinGameStateTick();
                break;
            case ZombiesGameState.HumanWin:
                this.humanWinGameStateTick();
                break;
            case ZombiesGameState.Ended:
                this.endedGameStateTick();
                break;
            default:
                break;
        }
    }

    private async Task zombieGameStateChanged(ZombiesGameState oldState) {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Game state changed from {oldState} to {this.State.GameState}");
        Console.ResetColor();

        switch (this.State.GameState) {
            case ZombiesGameState.WaitingForPlayers:
                await waitingForPlayersGameState();
                break;
            case ZombiesGameState.Countdown:
                await countdownGameState();
                break;
            case ZombiesGameState.BuildPhase:
                await buildPhaseGameState();
                break;
            case ZombiesGameState.GamePhase:
                await gamePhaseGameState();
                break;
            case ZombiesGameState.ZombieWin:
                await zombieWinGameState();
                break;
            case ZombiesGameState.HumanWin:
                await humanWinGameState();
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unhandled game state!");
                Console.ResetColor();
                break;
        }
    }
    #endregion

    #region GAME STATE HANDLERS
    #region GAME STATE CHANGED HANDLERS
    #region WAITING FOR PLAYERS
    private async Task waitingForPlayersGameState() {
        this.Server.RoundSettings.PlayersToStart = this.Configuration.RequiredPlayersToStart;

        foreach (RunnerPlayer player in this.Server.AllPlayers) {
            await this.applyWaitingForPlayersRuleSetToPlayer(player);
        }
    }
    #endregion

    #region COUNTDOWN
    private async Task countdownGameState() {
        //RunnerPlayer[] initialZombies = await initialZombiePopulation();
        //foreach (RunnerPlayer player in initialZombies)
        //{
        //    await this.makePlayerZombie(player);
        //}

        this.Server.RoundSettings.SecondsLeft = this.Configuration.CountdownPhaseDuration;
        this.Server.ServerSettings.CanVoteDay = Random.Shared.Next(5) == 0;
        this.Server.ServerSettings.CanVoteNight = true;

        foreach (RunnerPlayer player in this.Server.AllPlayers) {
            applyCountdownRuleSetToPlayer(player);
        }

        await Task.CompletedTask;
    }
    #endregion

    #region BUILD PHASE
    private async Task buildPhaseGameState() {
        // Ruleset for build phase:
        // - All human squads receive BuildPhaseSquadPoints build points
        // - All squads can not make squad points by capturing/killing
        // - All zombie squads have 0 build points

        this.State.BuildPhase = true;
        this.State.EndOfBuildPhase = DateTime.Now.AddSeconds(this.Configuration.BuildPhaseDuration);

        this.Server.AnnounceLong($"Human build phase has started! Infection in {this.Configuration.BuildPhaseDuration} seconds!");

        foreach (RunnerPlayer player in this.Server.AllPlayers) {
            await this.applyBuildPhaseRuleSetToPlayer(player);
        }

        foreach (Squad<RunnerPlayer> squad in this.Server.AllSquads) {
            if (squad.Team == HUMANS) {
                squad.SquadPoints = this.Configuration.BuildPhaseSquadPoints;
                this.setLastHumanSquadPoints(squad.Name, squad.SquadPoints);
            } else {
                squad.SquadPoints = 0;
            }
        }
    }

    private void buildPhaseManagement() {
        if (this.State.GameState != ZombiesGameState.BuildPhase && this.State.BuildPhase) {
            this.State.BuildPhase = false;
            return;
        }

        if (this.State.EndOfBuildPhase <= DateTime.Now) {
            this.State.BuildPhase = false;
            this.State.EndOfBuildPhase = DateTime.MinValue;
            return;
        }

        int secondsLeft = (int)(this.State.EndOfBuildPhase - DateTime.Now).TotalSeconds;

        if ((secondsLeft % 10) == 0 && this.State.LastZombiesArrivalAnnounced != secondsLeft) {
            this.State.LastZombiesArrivalAnnounced = secondsLeft;
            this.Server.SayToAllChat($"Infection in {secondsLeft} seconds!");
        }

        if (this.State.EndOfBuildPhase.AddSeconds(-10) < DateTime.Now) {
            this.Server.AnnounceShort($"Infection in {this.State.EndOfBuildPhase.Subtract(DateTime.Now).TotalSeconds:0} seconds!");
        }

        foreach (RunnerPlayer player in this.Server.AllPlayers.Where(p => p.Team != HUMANS)) {
            player.ChangeTeam(HUMANS);
        }
    }
    #endregion

    #region GAME PHASE
    private async Task gamePhaseGameState() {
        // Ruleset for game phase:
        // - All human squads will have their squad points set to GamePhaseSquadPoints
        // - Squads can not make squad points by capturing/killing
        // - Human squad points are set to GamePhaseSquadPoints
        // - Zombies can deploy
        // - Zombies are unfrozen
        // - A random population of humans will turn into zombies in the midst of the humans at a random time interval between 0 and ZombieMaxInfectionTime ms

        this.Server.RoundSettings.SecondsLeft = this.Configuration.GamePhaseDuration;

        foreach (Squad<RunnerPlayer> squad in this.Server.AllSquads.Where(s => s.Team == HUMANS)) {
            squad.SquadPoints = this.Configuration.GamePhaseSquadPoints;
            this.setLastHumanSquadPoints(squad.Name, squad.SquadPoints);
        }

        this.Server.AnnounceShort($"The infection is starting!");

        RunnerPlayer[] initialZombies = await initialZombiePopulation();
        foreach (RunnerPlayer player in initialZombies) {
            _ = Task.Run(async () => {
                await Task.Delay(Random.Shared.Next(this.Configuration.ZombieMaxInfectionTime));
                if (player.Position.X != 0 && player.Position.Y != 0 && player.Position.Z != 0) {
                    this.getPlayer(player).Persistence.SpawnPosition = player.Position;
                }
                await this.makePlayerZombie(player);
            });
        }
    }
    #endregion

    #region END OF GAME
    private async Task zombieWinGameState() {
        this.Server.AnnounceShort($"{RichText?.FromColorName("red")}ZOMBIES WIN");
        this.Server.ForceEndGame(ZOMBIES);
        this.State.GameState = ZombiesGameState.Ended;

        await Task.CompletedTask;
    }

    private async Task humanWinGameState() {
        this.Server.AnnounceShort($"{RichText?.FromColorName("blue")}HUMANS WIN");
        this.Server.ForceEndGame(HUMANS);
        this.State.GameState = ZombiesGameState.Ended;

        await Task.CompletedTask;
    }

    private bool isZombieWin() {
        if (this.actualHumanCount == 0) {
            return true;
        }

        return false;
    }

    private bool isHumanWin() {
        if (this.Server.RoundSettings.SecondsLeft <= 2 || this.State.ZombieTickets <= 3) {
            return true;
        }

        return false;
    }
    #endregion

    #endregion

    #region GAME STATE TICK HANDLERS
    private void endedGameStateTick() {
    }

    private void humanWinGameStateTick() {
    }

    private void zombieWinGameStateTick() {
    }

    private void gamePhaseGameStateTick() {
        this.exposeHumansOnMap();
        this.announceHumanCount();

        this.humanLoadoutHandler();
        this.zombieLoadoutHandler();

        this.ensureZombiesHaveNoGuns();

        this.zombieTicketHandler();

        this.exclusionZoneHandler();
    }

    private List<Vector2[]> exclusionZones = new List<Vector2[]>();
    private string? currentExclusionZoneMap = null;

    private void exclusionZoneHandler() {
        string exclusionZoneMapFileName = $"{this.Server.Map}-{this.Server.MapSize}.json";
        if (this.currentExclusionZoneMap != exclusionZoneMapFileName) {
            this.exclusionZones.Clear();
            this.currentExclusionZoneMap = exclusionZoneMapFileName;
            try {
                this.exclusionZones = JsonSerializer.Deserialize<List<SimplePoint[]>>(File.ReadAllText($"./data/Zombies/exclusionZones/{exclusionZoneMapFileName}"))?.Select(v => v.Select(a => new Vector2(a.X, a.Y)).ToArray()).ToList() ?? new();
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to load exclusion zones for map {exclusionZoneMapFileName}: {ex.Message}");
                Console.ResetColor();
            }
            Console.WriteLine($"Loaded {this.exclusionZones.Count} exclusion zones from {exclusionZoneMapFileName}");
        }

        Stopwatch exclusionZoneMeasurement = Stopwatch.StartNew();
        foreach (Vector2[] exclusionZone in this.exclusionZones) {
            foreach (RunnerPlayer player in this.Server.AllPlayers.Where(p => p.Team == HUMANS && p.IsAlive)) {
                if (player.Position.X != 0 && player.Position.Y != 0 && player.Position.Z != 0) {
                    if (!IsPointInPolygon(exclusionZone, new() {
                        X = player.Position.X,
                        Y = player.Position.Z
                    })) {
                        continue;
                    }

                    if (this.State.GameState == ZombiesGameState.BuildPhase) {
                        player.Message($"{this.RichText?.FromColorName("red")}{this.RichText?.Size(125)}/!\\ATTENTION/!\\{this.RichText?.NewLine() ?? " "}{this.RichText?.Color()}{this.RichText?.Size(100)}You are currently inside or very close to a safe zone or water.{this.RichText?.NewLine() ?? " "}{this.RichText?.FromColorName("yellow")}YOU WILL BE KILLED IF YOU STAY HERE ONCE THE BUILD PHASE HAS ENDED.", 1);
                        continue;
                    }

                    ZombiesPlayer zombiesPlayer = this.getPlayer(player);

                    zombiesPlayer.Persistence.ExclusionZoneWarningThreshold++;

                    if (zombiesPlayer.Persistence.ExclusionZoneWarningThreshold > this.Configuration.ExclusionZoneWarningThreshold) {
                        player.Kill();
                        player.Message("You have been in the exclusion zone for too long.");
                    } else {
                        player.Message($"{this.RichText?.FromColorName("red")}{this.RichText?.Size(125)}/!\\ATTENTION/!\\{this.RichText?.NewLine() ?? " "}{this.RichText?.Color()}{this.RichText?.Size(100)}You are currently inside or very close to a safe zone or water.{this.RichText?.NewLine() ?? " "}{this.RichText?.FromColorName("yellow")}MOVE AWAY FROM THE EXCLUSION ZONE OR YOU WILL BE KILLED.", 1);
                    }
                }
            }
        }

        exclusionZoneMeasurement.Stop();
        if (exclusionZoneMeasurement.ElapsedMilliseconds > 50) {
            Console.WriteLine($"Exclusion zone measurement takes too long: {exclusionZoneMeasurement.ElapsedMilliseconds}ms");
        }
    }

    public static bool IsPointInPolygon(Vector2[] polygon, Vector2 point) {
        if (polygon.Length < 3)
            return false;

        int count = 0;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++) {
            if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)) {
                count++;
            }
        }

        return count % 2 == 1;
    }


    private void ensureZombiesHaveNoGuns() {
        foreach (RunnerPlayer player in this.Server.AllPlayers.Where(p => p.Team == ZOMBIES)) {
            if (!string.IsNullOrEmpty(player.CurrentLoadout.PrimaryWeapon.ToolName) && player.CurrentLoadout.PrimaryWeapon.ToolName != "EmptyGun") {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Player {player.Name} has a {player.CurrentLoadout.PrimaryWeapon.ToolName}!");
                Console.ResetColor();
                player.SetHeavyGadget(Gadgets.SledgeHammer.Name, 1, true);
                player.SetLightGadget(Gadgets.Pickaxe.Name, 1, true);
                player.Kill();
                player.Message("Sorry loadout bug, you were killed to fix it.");
            }
        }
    }

    private void zombieTicketHandler() {
        this.Server.RoundSettings.TeamBTickets = this.State.ZombieTickets;
    }

    private void buildPhaseGameStateTick() {
        this.buildPhaseManagement();
        this.zombieLoadoutHandler();
        this.exclusionZoneHandler();
    }

    private void countdownGameStateTick() {
    }

    private void waitingForPlayersGameStateTick() {
    }
    #endregion
    #endregion

    #region SERVER CALLBACKS
    public override void OnModulesLoaded() {
        this.CommandHandler.Register(this);
    }

    public override async Task OnConnected() {
        await this.applyServerSettings();

        _ = Task.Run(gameStateManagerWorker);

        await Task.CompletedTask;
    }

    public override Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole) {
        if (requestedRole == GameRole.Support || requestedRole == GameRole.Engineer) {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public override async Task OnPlayerConnected(RunnerPlayer player) {
        // Ruleset:
        // - Enforce team based on game state
        // - Enforce ruleset based on game state

        if (this.State.GameState == ZombiesGameState.BuildPhase ||
            this.State.GameState == ZombiesGameState.Countdown ||
            this.State.GameState == ZombiesGameState.WaitingForPlayers) {
            if (player.Team != HUMANS) {
                player.ChangeTeam(HUMANS);
            }

            if (!player.InSquad) {
                Squad<RunnerPlayer> targetSquad = await this.findFirstNonEmptySquad(player.Team);
                player.JoinSquad(targetSquad.Name);
            }

            await this.applyHumanRuleSetToPlayer(player);
            await this.applyBuildPhaseRuleSetToPlayer(player);

            return;
        }

        if (this.State.GameState == ZombiesGameState.GamePhase) {
            if (player.Team == HUMANS) {
                player.ChangeTeam(ZOMBIES);
            }

            await this.applyZombieRuleSetToPlayer(player);

            return;
        }
    }

    public override Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam) {
        // Ruleset:
        // - Do not allow players to change teams

        return Task.FromResult(false);
    }

    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args) {
        // Ruleset:
        // - Set player rank to 200
        // - Set player prestige to 10

        args.Stats.Progress.Rank = 200;
        args.Stats.Progress.Prestige = 10;
        return Task.CompletedTask;
    }

    public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request) {
        if (player.Team == HUMANS) {
            // Ruleset for humans:
            // - Set randomized human survivor skin
            // - If night, force flashlight on primary and secondary but keep existing flashlight
            // - Apply human ruleset based on game state

            // Human skins
            request.Wearings.Uniform = HUMAN_UNIFORM[Random.Shared.Next(HUMAN_UNIFORM.Length)];
            request.Wearings.Head = HUMAN_HELMET[Random.Shared.Next(HUMAN_HELMET.Length)];
            request.Wearings.Backbag = HUMAN_BACKPACK[Random.Shared.Next(HUMAN_BACKPACK.Length)];
            request.Wearings.Chest = HUMAN_ARMOR[Random.Shared.Next(HUMAN_ARMOR.Length)];

            this.getPlayer(player).InitialLoadout = request.Loadout;

            if (this.Server.DayNight == MapDayNight.Night) {
                if (!HUMAN_FLASHLIGHTS.Contains(request.Loadout.PrimaryWeapon.SideRailName)) {
                    request.Loadout.PrimaryWeapon.SideRail = new Attachment(HUMAN_FLASHLIGHTS[Random.Shared.Next(HUMAN_FLASHLIGHTS.Length)], AttachmentType.SideRail);
                }

                if (!HUMAN_FLASHLIGHTS.Contains(request.Loadout.SecondaryWeapon.SideRailName)) {
                    request.Loadout.SecondaryWeapon.SideRail = new Attachment(HUMAN_FLASHLIGHTS[Random.Shared.Next(HUMAN_FLASHLIGHTS.Length)], AttachmentType.SideRail);
                }
            }

            request.Loadout.SecondaryExtraMagazines = 15;

            switch (this.State.GameState) {
                case ZombiesGameState.BuildPhase:
                    await this.applyBuildPhaseRuleSetToPlayer(player);
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Human spawned during game state {this.State.GameState} which is ignored.");
                    Console.ResetColor();
                    break;
            }

            if (this.State.GameState == ZombiesGameState.Countdown ||
                this.State.GameState == ZombiesGameState.BuildPhase) {

                request.Loadout.Throwable = null;

                request.Loadout.PrimaryWeapon.Tool = null;

                request.Loadout.LightGadget = null;

                request.Loadout.HeavyGadget = null;

            } else if (this.State.GameState == ZombiesGameState.GamePhase) {
                // Only allow for humans to spawn with the loadout they are supposed to have
                double humanRatio = (double)this.actualHumanCount / this.Server.AllPlayers.Count();

                ZombiesPlayer human = this.getPlayer(player);

                if (humanRatio <= this.Configuration.HumanRatioThrowable) {
                    human.Persistence.ReceivedThrowable = true;
                } else {
                    request.Loadout.Throwable = null;
                }

                if (humanRatio <= this.Configuration.HumanRatioPrimary) {
                    human.Persistence.ReceivedPrimary = true;
                } else {
                    request.Loadout.PrimaryWeapon.Tool = null;
                }

                if (humanRatio <= this.Configuration.HumanRatioLightGadget) {
                    human.Persistence.ReceivedLightGadget = true;
                } else {
                    request.Loadout.LightGadget = null;
                }

                if (humanRatio <= this.Configuration.HumanRatioHeavyGadget) {
                    human.Persistence.ReceivedHeavyGadget = true;
                } else {
                    request.Loadout.HeavyGadget = null;
                }
            }

            return request;
        }

        // Ruleset for zombies:
        // - Set zombie skin
        // - Apply loadout for zombie
        // - Apply zombie ruleset based on game state

        request.Wearings.Eye = ZOMBIE_EYES[Random.Shared.Next(ZOMBIE_EYES.Length)];
        request.Wearings.Face = ZOMBIE_FACE[Random.Shared.Next(ZOMBIE_FACE.Length)];
        request.Wearings.Hair = ZOMBIE_HAIR[Random.Shared.Next(ZOMBIE_HAIR.Length)];
        request.Wearings.Skin = ZOMBIE_BODY[Random.Shared.Next(ZOMBIE_BODY.Length)];
        request.Wearings.Uniform = ZOMBIE_UNIFORM[Random.Shared.Next(ZOMBIE_UNIFORM.Length)];
        request.Wearings.Head = ZOMBIE_HELMET[Random.Shared.Next(ZOMBIE_HELMET.Length)];
        request.Wearings.Chest = ZOMBIE_ARMOR[Random.Shared.Next(ZOMBIE_ARMOR.Length)];
        request.Wearings.Backbag = ZOMBIE_BACKPACK[Random.Shared.Next(ZOMBIE_BACKPACK.Length)];
        request.Wearings.Belt = ZOMBIE_BELT[Random.Shared.Next(ZOMBIE_BELT.Length)];

        request.Loadout.FirstAid = default;
        request.Loadout.FirstAidExtra = 0;
        request.Loadout.PrimaryWeapon = default;
        request.Loadout.SecondaryWeapon = default;
        request.Loadout.Throwable = Gadgets.SmokeGrenadeRed; // Red smoke makes the zombies menacing
        request.Loadout.ThrowableExtra = 1;
        request.Loadout.LightGadget = Gadgets.SledgeHammer;
        request.Loadout.HeavyGadget = Gadgets.Pickaxe;

        return request;
    }

    public override async Task OnPlayerSpawned(RunnerPlayer player) {

        if (player.Team == HUMANS) {
            await this.applyHumanRuleSetToPlayer(player);

            return;
        }

        await this.applyZombieRuleSetToPlayer(player);
        this.State.ZombieTickets--;
        if (this.State.ZombieTickets < 200 && this.State.ZombieTickets % 50 == 0) {
            this.Server.SayToAllChat($"{this.RichText?.Size(125)}{this.RichText?.FromColorName("yellow")}Zombies have {this.State.ZombieTickets} tickets left!");
        }

        Vector3? spawnPosition = this.getPlayer(player).Persistence.SpawnPosition;
        if (spawnPosition is not null) {
            this.getPlayer(player).Persistence.SpawnPosition = null;
            player.Teleport(spawnPosition.Value);
        }
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player) {
        if (this.players.ContainsKey(player.SteamID)) {
            this.players.Remove(player.SteamID);
        }

        return Task.CompletedTask;
    }

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args) {
        if (args.Victim.Team == HUMANS) {
            this.getPlayer(args.Victim).Persistence.SpawnPosition = args.Victim.Position;
        }

        return Task.CompletedTask;
    }

    public override async Task OnPlayerDied(RunnerPlayer player) {
        if (player.Team == ZOMBIES) {
            this.getPlayer(player).Persistence.ZombieClass = null;
            return;
        }

        if (this.State.GameState != ZombiesGameState.GamePhase) {
            return;
        }

        await this.makePlayerZombie(player);
        this.Server.UILogOnServer($"{player.Name} has been turned into a zombie!", 10);
        this.DiscordWebhooks?.SendMessage($"{player.Name} has been turned into a zombie! ({this.Server.AllPlayers.Count(p => p.Team == ZOMBIES)}/{this.Server.AllPlayers.Count(p => p.Team == HUMANS && p.IsAlive && !p.IsDown)} z/h ratio)");
        await Task.CompletedTask;
    }

    public override Task OnSessionChanged(long oldSessionID, long newSessionID) {
        this.State.Reset();
        this.players.Clear();

        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
        if (player.SteamID == 76561198142010443) {
            if (msg.StartsWith("!")) {
                return Task.FromResult(false);
            }

            if (channel != ChatChannel.AllChat) {
                return Task.FromResult(true);
            }

            this.Server.SayToAllChat($"{this.RichText?.Size(110)}{this.RichText?.FromColorName("yellow")}BiG|Rain{this.RichText?.FromColorName("BlueViolet")}[Server Dev]{this.RichText?.Color()}: {msg}");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints) {
        // Ruleset for squad point changes:
        // - Zombies can never have squad points
        // - Humans can not make squad points by capturing/killing

        if (squad.Team == ZOMBIES) {
            squad.SquadPoints = 0;
            return;
        }

        if (this.getLastHumanSquadPoints(squad.Name) < newPoints) {
            squad.SquadPoints = this.getLastHumanSquadPoints(squad.Name);
            return;
        }

        this.setLastHumanSquadPoints(squad.Name, newPoints);

        await Task.CompletedTask;
    }
    #endregion

    #region HELPER METHODS
    private int actualHumanCount => this.Server.AllPlayers.Count(player => player.Team == HUMANS && !player.IsDown && player.IsAlive);

    private Task<RunnerPlayer[]> initialZombiePopulation() {
        List<RunnerPlayer> zombies = new();

        // Initial zombies selection
        List<RunnerPlayer> players = this.Server.AllPlayers.Where(p => p.Team == HUMANS && p.IsAlive).ToList();
        int initialZombieCount = this.Configuration.InitialZombieCount;

        // If initial zombie count is greater than initial zombie maximum percentage, set it to the maximum percentage
        int maxAmountOfZombies = (int)Math.Ceiling(players.Count * (this.Configuration.InitialZombieMaxPercentage / 100.0));
        if (initialZombieCount > maxAmountOfZombies) {
            initialZombieCount = maxAmountOfZombies;
        }

        Console.WriteLine($"{maxAmountOfZombies} / {initialZombieCount} / {this.Configuration.InitialZombieMaxPercentage}");

        for (int i = 0; i < initialZombieCount; i++) {
            // TODO: maybe add ability for players to pick a preference of being a zombie or human
            int randomIndex = Random.Shared.Next(players.Count);
            RunnerPlayer player = players[randomIndex];
            players.RemoveAt(randomIndex);
            zombies.Add(player);
        }

        return Task.FromResult(zombies.ToArray());
    }

    private void debugLog(string message) {
        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] {message}");
    }

    private int getLastHumanSquadPoints(Squads humanSquad) {
        if (!this.State.LastSquadPoints.ContainsKey(humanSquad)) {
            this.State.LastSquadPoints.Add(humanSquad, 0);
        }

        return this.State.LastSquadPoints[humanSquad];
    }

    private void setLastHumanSquadPoints(Squads humanSquad, int points) {
        if (!this.State.LastSquadPoints.ContainsKey(humanSquad)) {
            this.State.LastSquadPoints.Add(humanSquad, 0);
        }

        this.State.LastSquadPoints[humanSquad] = points;
    }

    private Task<Squad<RunnerPlayer>> findFirstNonEmptySquad(Team team) {
        foreach (Squad<RunnerPlayer> squad in this.Server.AllSquads.Where(s => s.Team == team)) {
            if (squad.NumberOfMembers < 8) {
                return Task.FromResult(squad);
            }
        }

        // No free squads available, this is impossible (8 players * 64 squads = 512 players which is more than the max player count of 254)
        throw new Exception("No free squads available");
    }

    private async Task makePlayerZombie(RunnerPlayer player) {
        if (player.Team == ZOMBIES) {
            return;
        }

        PlayerLoadout oldLoadout = player.CurrentLoadout;
        PlayerWearings oldWearings = player.CurrentWearings;
        Vector3 oldPosition = player.Position;
        player.ChangeTeam(ZOMBIES);
        player.SpawnPlayer(oldLoadout, oldWearings, oldPosition, new Vector3(0, 0, 0), PlayerStand.Proning, 0f);
        player.Message($"You have been turned into a {RichText?.FromColorName("red")}ZOMBIE{RichText?.FromColorName("white")}.", 10);

        await Task.CompletedTask;
    }
    #endregion

    #region RULE SETS
    #region GAME STATE RULE SETS
    private async Task applyWaitingForPlayersRuleSetToPlayer(RunnerPlayer player) {
        // Ruleset for waiting for players:
        // - All players are human
        // - All players can not deploy
        // - All players are frozen

        player.ChangeTeam(HUMANS);
        player.Modifications.CanDeploy = true;
        player.Modifications.Freeze = false;

        await Task.CompletedTask;
    }

    private void applyCountdownRuleSetToPlayer(RunnerPlayer player) {
        // Ruleset for countdown:
        // - Humans can deploy
        // - Humans are unfrozen

        if (player.Team == HUMANS) {
            player.Modifications.CanDeploy = true;
            player.Modifications.Freeze = false;
        }
    }

    private async Task applyBuildPhaseRuleSetToPlayer(RunnerPlayer player) {
        // Ruleset for build phase:
        // - Humans can deploy
        // - Humans are unfrozen
        // - Zombies can not deploy
        // - Zombies are frozen
        // - All humans must be in a squad

        if (player.Team == HUMANS) {
            await this.applyHumanRuleSetToPlayer(player);

            player.Modifications.CanDeploy = true;
            player.Modifications.Freeze = false;

            if (!player.InSquad) {
                Squad<RunnerPlayer> targetSquad = await this.findFirstNonEmptySquad(player.Team);
                player.JoinSquad(targetSquad.Name);
            }
        } else if (player.Team == ZOMBIES) {
            await this.applyZombieRuleSetToPlayer(player);

            player.Modifications.CanDeploy = true;
            player.Modifications.Freeze = false;
        }
    }
    #endregion

    #region PLAYER RULE SETS
    private async Task applyHumanRuleSetToPlayer(RunnerPlayer player) {
        //Console.WriteLine($"Human rule for {player.Name}");
        // Ruleset for humans:
        // - Humans are all default
        // - Humans can not suicide
        // - Humans can not use vehicles
        // - Humans can not use NVGs
        // - Humans do not see friendly HUD indicators
        // - Humans do not have hitmarkers
        // - Humans can not heal using bandages
        // - Humans can not revive
        // - Humans can spawn anywhere except vehicles

        player.Modifications.AirStrafe = true;
        player.Modifications.AllowedVehicles = VehicleType.None;
        player.Modifications.CanUseNightVision = false;
        player.Modifications.FallDamageMultiplier = 1f;
        player.Modifications.FriendlyHUDEnabled = false;
        player.Modifications.GiveDamageMultiplier = 1f;
        player.Modifications.HitMarkersEnabled = false;
        player.Modifications.HpPerBandage = this.Configuration.HpPerBandage;
        player.Modifications.JumpHeightMultiplier = 1f;
        player.Modifications.MinimumDamageToStartBleeding = 10f;
        player.Modifications.MinimumHpToStartBleeding = 40f;
        player.Modifications.ReceiveDamageMultiplier = 1f;
        player.Modifications.ReloadSpeedMultiplier = 1f;
        if (player.Role == GameRole.Medic) {
            player.Modifications.ReviveHP = 30;
        } else {
            player.Modifications.ReviveHP = 0;
        }
        player.Modifications.RespawnTime = 0;
        player.Modifications.RunningSpeedMultiplier = 1f;
        player.Modifications.DownTimeGiveUpTime = 13;
        player.Modifications.SpawningRule = HUMANS_SPAWN_RULE;
        player.Modifications.CanSuicide = false;

        player.Modifications.CanDeploy = true;
        player.Modifications.Freeze = false;

        await Task.CompletedTask;
    }

    private async Task applyZombieRuleSetToPlayer(RunnerPlayer player) {
        //Console.WriteLine($"Zombie rule for {player.Name}");
        // Ruleset for zombies:
        // - Zombies can not have a primary weapon
        // - Zombies can not have a secondary weapon
        // - Zombies can not have a gadget
        // - Zombies must have a melee weapon
        // - Zombies can only have smoke grenades
        // - Zombies can not have bandages
        // - Zombies can not revive or heal
        // - Zombies do not bleed
        // - Zombies are faster
        // - Zombies jump higher
        // - Zombies can suicide
        // - Zombies can not use vehicles
        // - Zombies can use NVGs
        // - Zombies have adjusted incoming damage
        // - Zombies can see friendly HUD indicators
        // - Zombies have hitmarkers
        // - Zombies may have classes that change these rules
        // - Zombies can only spawn on points and squad mates

        if (player.CurrentLoadout.PrimaryWeapon.ToolName is not null && player.CurrentLoadout.PrimaryWeapon.ToolName != "EmptyGun") {
            await Console.Out.WriteLineAsync($"Zombie {player.Name} has gun {player.CurrentLoadout.PrimaryWeapon.ToolName}, resetting loadout");
            player.SetFirstAidGadget("none", 0);
            player.SetHeavyGadget(Gadgets.SledgeHammer.Name, 0, true);
        }

        if (!allowedZombieMeleeGadgets.Contains(player.CurrentLoadout.HeavyGadgetName)) {
            player.SetHeavyGadget(Gadgets.SledgeHammer.Name, 0, true);
        }

        if (!allowedZombieMeleeGadgets.Contains(player.CurrentLoadout.LightGadgetName)) {
            player.SetLightGadget(Gadgets.Pickaxe.Name, 0);
        }

        if (!allowedZombieThrowables.Contains(player.CurrentLoadout.ThrowableName)) {
            player.SetThrowable(Gadgets.SmokeGrenadeRed.Name, 5);
        }

        player.Modifications.AirStrafe = true;
        player.Modifications.AllowedVehicles = VehicleType.None;
        player.Modifications.CanUseNightVision = true;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.FriendlyHUDEnabled = true;
        player.Modifications.GiveDamageMultiplier = 1f;
        player.Modifications.HitMarkersEnabled = true;
        player.Modifications.HpPerBandage = 0f;
        player.Modifications.JumpHeightMultiplier = this.Configuration.ZombieJumpHeightMultiplier;
        player.Modifications.MinimumDamageToStartBleeding = 100f;
        player.Modifications.MinimumHpToStartBleeding = 0;
        player.Modifications.ReceiveDamageMultiplier = this.Configuration.ZombieDamageReceived;
        player.Modifications.ReloadSpeedMultiplier = 1f;
        player.Modifications.ReviveHP = 0;
        player.Modifications.RespawnTime = 0;
        player.Modifications.RunningSpeedMultiplier = this.Configuration.ZombieRunningSpeedMultiplier;
        player.Modifications.DownTimeGiveUpTime = 5;
        player.Modifications.SpawningRule = ZOMBIES_SPAWN_RULE;
        player.Modifications.CanSuicide = true;

        float ratio = (float)this.Server.AllPlayers.Count(p => p.Team == ZOMBIES) / ((float)this.Server.AllPlayers.Count() - 1);
        float multiplier = (float)this.Configuration.ZombieMinDamageReceived + ((float)this.Configuration.ZombieMaxDamageReceived - (float)this.Configuration.ZombieMinDamageReceived) * (float)(ratio - 0.1f);
        player.Modifications.ReceiveDamageMultiplier = multiplier;

        if (this.State.GameState == ZombiesGameState.GamePhase) {
            player.Modifications.CanDeploy = true;
            player.Modifications.Freeze = false;
        } else {
            player.Modifications.CanDeploy = true;
            player.Modifications.Freeze = false;
        }

        await Task.CompletedTask;
    }
    #endregion
    #endregion

    #region LOADOUT HANDLERS
    private void zombieLoadoutHandler() {
        // Ruleset:
        // - Maximum of ZombieClassRatio % of zombies can have a class
        // - If there are more zombies than the ratio, the rest will be normal zombies
        // - Classes are limited by amount of zombies with that class

        ZombiesPlayer[] classedZombies = this.Server.AllPlayers.Where(p => p.Team == ZOMBIES && !p.IsDown && p.IsAlive).Select(p => this.getPlayer(p)).Where(p => p.Persistence.ZombieClass is not null).ToArray();

        List<ZombieClass> availableClasses = new();
        foreach (ZombieClass @class in zombieClasses) {
            float percentOfClassed = classedZombies.Count(z => z.Persistence.ZombieClass == @class.Name) / ((float)classedZombies.Length == 0 ? 1f : (float)classedZombies.Length);
            //Console.WriteLine($"Class has {percentOfClassed} of the total zombies and needs {@class.RequestedPercentage}");

            if (@class.RequestedPercentage <= 10 && this.Server.AllPlayers.Count(p => p.Team == ZOMBIES) < this.Server.AllPlayers.Count(p => p.Team == HUMANS && p.IsAlive && !p.IsDown)) {
                continue;
            }

            if (percentOfClassed < (float)@class.RequestedPercentage / 100f) {
                availableClasses.Add(@class);
            }
        }

        //ZombieClass[] availableClasses = zombieClasses.Where(c => classedZombies.Count(z => z.Loadout.ZombieClass == c.Name) < c.RequestedPercentage).ToArray();

        //Console.WriteLine($"The following classes are available:{Environment.NewLine}{string.Join(Environment.NewLine, availableClasses.Select(c => $"{c.Name} ({c.RequestedAmount - classedZombies.Count(cls => cls.Loadout.ZombieClass == c.Name)})"))}");

        if (availableClasses.Count() == 0) {
            return;
        }

        double classedZombiesRatio = (double)classedZombies.Length / this.Server.AllPlayers.Count(player => player.Team == ZOMBIES);

        //Console.WriteLine($"The ratio is {classedZombiesRatio} >= {this.Configuration.ZombieClassRatio}");

        if (classedZombiesRatio >= this.Configuration.ZombieClassRatio) {
            return;
        }

        ZombiesPlayer[] classCandidates = this.Server.AllPlayers.Where(p => p.Team == ZOMBIES && !p.IsDown && p.IsAlive).Select(p => this.getPlayer(p)).Where(p => p.Persistence.ZombieClass is null).ToArray();
        if (classCandidates.Length == 0) {
            return;
        }

        ZombiesPlayer candidate = classCandidates[Random.Shared.Next(classCandidates.Length)];
        ZombieClass zombieClass = availableClasses[Random.Shared.Next(availableClasses.Count)];

        candidate.Persistence.ZombieClass = zombieClass.Name;
        zombieClass.ApplyToPlayer(candidate.Player);
        candidate.Player.Message($"{this.RichText?.Size(120)}{this.RichText?.FromColorName("yellow")}YOU HAVE MUTATED!{this.RichText?.NewLine() ?? " "}{this.RichText?.Size(100)}{this.RichText?.Color()}You are a {this.RichText?.FromColorName("red")}{zombieClass.Name} {this.RichText?.Color()}zombie now! Go fuck shit up!", 15);
        this.Server.UILogOnServer($"{candidate.Player.Name} has mutated into a {zombieClass.Name} zombie.", 10);
        this.DiscordWebhooks?.SendMessage($"Player {candidate.Player.Name} has mutated into a {zombieClass.Name} zombie.");
    }

    private void humanLoadoutHandler() {
        foreach (RunnerPlayer player in this.Server.AllPlayers.Where(p => p.Team == HUMANS)) {
            ZombiesPlayer human = this.getPlayer(player);
            if (!human.Persistence.ReceivedPrimary || !human.Persistence.ReceivedHeavyGadget || !human.Persistence.ReceivedLightGadget || !human.Persistence.ReceivedThrowable) {
                this.applyHumanLoadoutToPlayer(player);
            }
        }
    }

    private void applyHumanLoadoutToPlayer(RunnerPlayer player) {
        double humanRatio = (double)this.actualHumanCount / this.Server.AllPlayers.Count();

        //Console.WriteLine($"Human ratio is {humanRatio}");

        ZombiesPlayer human = this.getPlayer(player);
        if (human.InitialLoadout == null) {
            human.InitialLoadout = new() {
                HeavyGadget = Gadgets.GrapplingHook,
                HeavyGadgetExtra = 1,
                LightGadget = Gadgets.SmallAmmoKit,
                LightGadgetExtra = 1,
                PrimaryWeapon = new WeaponItem() {
                    Barrel = Attachments.Compensator,
                    BoltAction = null,
                    CantedSight = null,
                    MainSight = Attachments.RedDot,
                    SideRail = this.Server.DayNight == MapDayNight.Night ? new Attachment(HUMAN_FLASHLIGHTS[Random.Shared.Next(HUMAN_FLASHLIGHTS.Length)], AttachmentType.SideRail) : Attachments.Redlaser,
                    Tool = Weapons.M4A1,
                    TopSight = null,
                    UnderRail = Attachments.VerticalGrip
                },
                PrimaryExtraMagazines = 4,
                Throwable = Gadgets.ImpactGrenade
            };
        }

        if (humanRatio <= this.Configuration.HumanRatioThrowable && !human.Persistence.ReceivedThrowable) {
            player.SayToChat($"{this.RichText?.FromColorName("green")}You received your throwable!");
            player.SetThrowable(human.InitialLoadout.Value.ThrowableName, 3);
            human.Persistence.ReceivedThrowable = true;
        }

        if (humanRatio <= this.Configuration.HumanRatioLightGadget && !human.Persistence.ReceivedLightGadget) {
            player.SayToChat($"{this.RichText?.FromColorName("green")}You received your light gadget!");

            if (human.InitialLoadout.Value.LightGadgetName == Gadgets.SmallAmmoKit) {
                player.SetLightGadget(Gadgets.HeavyAmmoKit.Name, human.InitialLoadout.Value.LightGadgetExtra);
            } else {
                player.SetLightGadget(human.InitialLoadout.Value.LightGadgetName, 2);
            }

            human.Persistence.ReceivedLightGadget = true;
        }

        if (humanRatio <= this.Configuration.HumanRatioHeavyGadget && !human.Persistence.ReceivedHeavyGadget) {
            player.SayToChat($"{this.RichText?.FromColorName("green")}You received your heavy gadget!");

            if (human.InitialLoadout.Value.HeavyGadgetName == Gadgets.SmallAmmoKit) {
                player.SetHeavyGadget(Gadgets.HeavyAmmoKit.Name, human.InitialLoadout.Value.HeavyGadgetExtra);
            } else {
                player.SetHeavyGadget(human.InitialLoadout.Value.HeavyGadgetName, 1);
            }

            human.Persistence.ReceivedHeavyGadget = true;
        }

        if (humanRatio <= this.Configuration.HumanRatioPrimary && !human.Persistence.ReceivedPrimary) {
            player.SayToChat($"{this.RichText?.FromColorName("green")}You received your primary weapon!");
            player.SetPrimaryWeapon(human.InitialLoadout.Value.PrimaryWeapon, human.InitialLoadout.Value.PrimaryExtraMagazines);
            human.Persistence.ReceivedPrimary = true;
        }
    }
    #endregion

    #region ACTIONS
    private void announceHumanCount() {
        int humanCount = this.actualHumanCount;

        if (this.State.LastHumansAnnounced > humanCount) {
            if (humanCount <= this.Configuration.AnnounceLastHumansCount) {
                if (humanCount == 1) {
                    RunnerPlayer? lastHuman = this.Server.AllPlayers.FirstOrDefault(p => p.Team == HUMANS && !p.IsDown && p.IsAlive);
                    if (lastHuman != null) {
                        this.Server.AnnounceShort($"<b>{lastHuman.Name}<b> is the LAST HUMAN, {this.RichText?.FromColorName("red")}KILL IT!");
                        this.Server.SayToAllChat($"<b>{lastHuman.Name}<b> is the LAST HUMAN, {this.RichText?.FromColorName("red")}KILL IT!");
                    } else {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No last human found");
                        Console.ResetColor();

                        this.Server.AnnounceShort($"LAST HUMAN, {this.RichText?.FromColorName("red")}KILL IT!");
                        this.Server.SayToAllChat($"{this.RichText?.Size(110)}LAST HUMAN, {this.RichText?.FromColorName("red")}KILL IT!");
                    }
                } else {
                    this.Server.SayToAllChat($"{humanCount} HUMANS LEFT, {this.RichText?.FromColorName("red")}KILL THEM!");
                }

                this.State.LastHumansAnnounced = humanCount;
            }
        }
    }

    private void exposeHumansOnMap() {
        if (this.Server.AllPlayers.Count(p => p.Team == HUMANS) <= this.Configuration.AnnounceLastHumansCount) {
            foreach (RunnerPlayer player in this.Server.AllPlayers) {
                if (player.Team == ZOMBIES) {
                    player.Modifications.IsExposedOnMap = false;
                    continue;
                }

                player.Modifications.IsExposedOnMap = true;
            }

            return;
        }

        if (DateTime.Now >= this.State.NextHumanExposeSwitch) {
            if (this.State.ExposeOnMap) {
                this.State.NextHumanExposeSwitch = DateTime.Now.AddSeconds(this.Configuration.HumanExposeOffTime);
                this.State.ExposeOnMap = false;
            } else {
                this.State.NextHumanExposeSwitch = DateTime.Now.AddSeconds(this.Configuration.HumanExposeOnTime);
                this.State.ExposeOnMap = true;
            }

            foreach (RunnerPlayer player in this.Server.AllPlayers) {
                if (player.Team == ZOMBIES) {
                    player.Modifications.IsExposedOnMap = false;
                    continue;
                }

                player.Modifications.IsExposedOnMap = this.State.ExposeOnMap;
            }
        }
    }


    private async Task applyServerSettings() {
        this.Server.RoundSettings.PlayersToStart = this.Configuration.RequiredPlayersToStart;
        this.Server.SetServerSizeForNextMatch(MapSize._127vs127);
        this.Server.MapRotation.SetRotation(new[] { "Azagor", "Construction", "District", "Dustydew", "Eduardovo", "Isle", "Lonovo", "Multuislands", "Namak", "Salhan", "Sandysunset", "Tensatown", "Valley", "Wakistan", "Wineparadise" });
        //this.Server.MapRotation.SetRotation(new[] { "Azagor", "District" });
        this.Server.GamemodeRotation.SetRotation("DOMI");
        this.Server.ServerSettings.UnlockAllAttachments = true;
        this.Server.ServerSettings.PlayerCollision = true;

        await Task.CompletedTask;
    }
    #endregion

    #region COMMANDS
    // Player commands

    [CommandCallback("fullgear", Description = "Gives you full gear", AllowedRoles = Roles.Admin)]
    public void FullGearCommand(RunnerPlayer player) {
        player.SetLightGadget(Gadgets.C4.Name, 1);
    }

    [CommandCallback("addtickets", Description = "Adds tickets to zombies", AllowedRoles = Roles.Admin)]
    public void AddTicketsCommand(RunnerPlayer player, int tickets) {
        this.State.ZombieTickets += tickets;
        player.Message($"Added {tickets} tickets to zombies");
    }

    [CommandCallback("list", Description = "List all players and their status")]
    public void ListCommand(RunnerPlayer player) {
        StringBuilder sb = new();
        sb.AppendLine("<b>==ZOMBIES==</b>");
        sb.AppendLine(string.Join(" / ", this.Server.AllPlayers.Where(p => p.Team == ZOMBIES).Select(p => $"{p.Name} is {(p.IsAlive ? "<color=\"green\">alive" : "<color=\"red\">dead")}<color=\"white\">")));
        sb.AppendLine("<b>==HUMANS==</b>");
        sb.AppendLine(string.Join(" / ", this.Server.AllPlayers.Where(p => p.Team == HUMANS).Select(p => $"{p.Name} is {(p.IsAlive ? "<color=\"green\">alive" : "<color=\"red\">dead")}<color=\"white\">")));
        player.Message(sb.ToString());
    }

    [CommandCallback("zombie", Description = "Check whether you're a zombie or not")]
    public void ZombieCommand(RunnerPlayer player) {
        this.Server.SayToAllChat($"{player.Name} is {(player.Team == ZOMBIES ? "a" : "not a")} zombie");
    }

    // Moderator/admin commands
    [CommandCallback("switch", Description = "Switch a player to the other team.", AllowedRoles = Roles.Moderator)]
    public async void SwitchCommand(RunnerPlayer source, RunnerPlayer target) {
        Team newTeam = target.Team == ZOMBIES ? HUMANS : ZOMBIES;
        target.Kill();
        target.ChangeTeam(newTeam);

        switch (this.State.GameState) {
            case ZombiesGameState.Countdown:
            case ZombiesGameState.BuildPhase:
            case ZombiesGameState.GamePhase:
                if (newTeam == ZOMBIES) {
                    await this.applyZombieRuleSetToPlayer(target);
                } else {
                    await this.applyHumanRuleSetToPlayer(target);
                }
                break;
        }
    }

    [CommandCallback("afk", Description = "Make zombies win because humans camp or are AFK", AllowedRoles = Roles.Moderator)]
    public async void LastHumanAFKOrCamping(RunnerPlayer caller) {
        if (this.Server.AllPlayers.Count(p => p.Team == HUMANS) > 10) {
            caller.Message("There are too many humans to end the game.");
            return;
        }

        this.Server.AnnounceLong("ZOMBIES WIN!");
        this.Server.ForceEndGame(ZOMBIES);
        this.DiscordWebhooks?.SendMessage($"== ZOMBIES WIN ==");

        return;
    }

    [CommandCallback("resetbuild", Description = "Reset the build phase.", AllowedRoles = Roles.Moderator)]
    public void ResetBuildCommand(RunnerPlayer caller) {
        this.State.GameState = ZombiesGameState.BuildPhase;
        this.State.BuildPhase = false;
        this.State.EndOfBuildPhase = DateTime.MinValue;

        caller.Message("Build phase reset.", 10);
        this.Server.SayToAllChat($"{this.RichText?.Size(125)}{this.RichText?.FromColorName("yellow")}Build phase aborted.");
    }

    [CommandCallback("map", Description = "Current map name")]
    public void MapCommand(RunnerPlayer caller) {
        caller.Message($"Current map: {this.Server.Map}");
    }

    [CommandCallback("pos", Description = "Current position", AllowedRoles = Roles.Admin)]
    public void PosCommand(RunnerPlayer caller) {
        caller.Message($"Current position: {caller.Position}", 5);

        File.AppendAllLines("positions.txt", new[] { $"{this.Server.Map},{this.Server.MapSize},{caller.Position.X}|{caller.Position.Y}|{caller.Position.Z}" });
    }
    #endregion
}

#region CLASSES AND ENUMS
public class ZombiesPlayer {
    public RunnerPlayer Player { get; set; }

    public ZombiesPlayer(RunnerPlayer player, ZombiePersistence loadout) {
        this.Player = player;
        this.Persistence = loadout;
    }

    public PlayerLoadout? InitialLoadout { get; set; } = null;

    public ZombiePersistence Persistence { get; set; }
}

public class ZombiePersistence {
    public bool ReceivedThrowable { get; set; } = false;
    public bool ReceivedLightGadget { get; set; } = false;
    public bool ReceivedHeavyGadget { get; set; } = false;
    public bool ReceivedPrimary { get; set; } = false;
    public string? ZombieClass { get; set; } = null;
    public Vector3? SpawnPosition { get; set; } = null;
    public int ExclusionZoneWarningThreshold { get; set; } = 0;
}

public class ZombiePersistenceStorage : ModuleConfiguration {
    public Dictionary<ulong, ZombiePersistence> Persistence { get; set; } = new();
}

public class ZombieClass {
    public string Name { get; }
    public float RequestedPercentage { get; }

    [JsonIgnore]
    public Action<RunnerPlayer> ApplyToPlayer { get; }

    public ZombieClass(string name, float requestedPercentage, Action<RunnerPlayer> applyToPlayer) {
        this.Name = name;
        this.RequestedPercentage = requestedPercentage;
        this.ApplyToPlayer = applyToPlayer;
    }
}

public class ZombiesConfiguration : ModuleConfiguration {
    public int InitialZombieCount { get; set; } = 6;
    public int InitialZombieMaxPercentage { get; set; } = 15;
    public int AnnounceLastHumansCount { get; set; } = 10;
    public int RequiredPlayersToStart { get; set; } = 20;
    public float ZombieDamageReceived { get; set; } = 2f;
    public float ZombieRunningSpeedMultiplier { get; set; } = 1f;
    public float ZombieJumpHeightMultiplier { get; set; } = 1f;
    public int BuildPhaseDuration { get; set; } = 120;
    public int GameStateUpdateTimer { get; set; } = 250;
    public int BuildPhaseSquadPoints { get; set; } = 500;
    public int GamePhaseSquadPoints { get; set; } = 0;
    public int ZombieMaxInfectionTime { get; set; } = 10000;
    public int GamePhaseDuration { get; set; } = 600;
    public int CountdownPhaseDuration { get; set; } = 10;
    public double HumanExposeOffTime { get; set; } = 6;
    public double HumanExposeOnTime { get; set; } = 2;
    public double HumanRatioThrowable { get; set; } = 0.65;
    public double HumanRatioLightGadget { get; set; } = 0.6;
    public double HumanRatioHeavyGadget { get; set; } = 0.55;
    public double HumanRatioPrimary { get; set; } = 0.5;
    public double ZombieClassRatio { get; set; } = 0.35;
    public float HpPerBandage { get; set; } = 40f;
    public float ZombieMinDamageReceived { get; set; }
    public float ZombieMaxDamageReceived { get; set; }
    public int ExclusionZoneWarningThreshold { get; set; } = 40;
}

public class ZombiesState : ModuleConfiguration {
    public ZombiesGameState GameState { get; set; } = ZombiesGameState.WaitingForPlayers;
    public Dictionary<Squads, int> LastSquadPoints { get; set; } = new();
    public int LastHumansAnnounced { get; set; } = int.MaxValue;
    public bool BuildPhase { get; set; } = false;
    public DateTime EndOfBuildPhase { get; set; } = DateTime.MinValue;
    public DateTime NextHumanExposeSwitch { get; set; } = DateTime.MinValue;
    public bool ExposeOnMap { get; set; } = false;
    public int LastZombiesArrivalAnnounced { get; set; } = 0;
    public double ZombieTickets { get; set; } = 600;

    public void Reset() {
        this.GameState = ZombiesGameState.Ended;
        this.LastSquadPoints = new();
        this.LastHumansAnnounced = int.MaxValue;
        this.BuildPhase = false;
        this.EndOfBuildPhase = DateTime.MinValue;
        this.NextHumanExposeSwitch = DateTime.MinValue;
        this.ExposeOnMap = false;
        this.LastZombiesArrivalAnnounced = 0;
        this.ZombieTickets = 600;

        this.Save();
    }
}

class SimplePoint {
    public float X { get; set; }
    public float Y { get; set; }
}

public enum ZombiesGameState {
    WaitingForPlayers,
    Countdown,
    BuildPhase,
    GamePhase,
    ZombieWin,
    HumanWin,
    Ended
}
#endregion
