# 0 Modules in Zombies.cs

| Description   | Version   |
|---------------|-----------|

## Commands
| Command    | Function Name   | Description                                     | Allowed Roles   | Parameters                                     | Defaults   |
|:-----------|:----------------|:------------------------------------------------|:----------------|:-----------------------------------------------|:-----------|
| fullgear   | void            | Gives you full gear                             | Admin           | ['RunnerPlayer player']                        | {}         |
| addtickets | void            | Adds tickets to zombies                         | Admin           | ['RunnerPlayer player', 'int tickets']         | {}         |
| list       | void            | List all players and their status               |                 | ['RunnerPlayer player']                        | {}         |
| zombie     | void            | Check whether you're a zombie or not            |                 | ['RunnerPlayer player']                        | {}         |
| switch     | async           | Switch a player to the other team.              | Moderator       | ['RunnerPlayer source', 'RunnerPlayer target'] | {}         |
| afk        | async           | Make zombies win because humans camp or are AFK | Moderator       | ['RunnerPlayer caller']                        | {}         |
| resetbuild | void            | Reset the build phase.                          | Moderator       | ['RunnerPlayer caller']                        | {}         |
| map        | void            | Current map name                                |                 | ['RunnerPlayer caller']                        | {}         |
| pos        | void            | Current position                                | Admin           | ['RunnerPlayer caller']                        | {}         |

## Public Methods
| Function Name       | Parameters                                                                         | Defaults   |
|:--------------------|:-----------------------------------------------------------------------------------|:-----------|
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| bool                | ['Vector2[] polygon', 'Vector2 point']                                             | {}         |
| void                | ['']                                                                               | {}         |
| async               | ['']                                                                               | {}         |
| Task                | ['RunnerPlayer player', 'GameRole requestedRole']                                  | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['RunnerPlayer player', 'Team requestedTeam']                                      | {}         |
| Task                | ['ulong steamID', 'PlayerJoiningArguments args']                                   | {}         |
| async               | ['RunnerPlayer player', 'OnPlayerSpawnArguments request']                          | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['OnPlayerKillArguments<RunnerPlayer> args']                                       | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['long oldSessionID', 'long newSessionID']                                         | {}         |
| Task                | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']                       | {}         |
| async               | ['Squad<RunnerPlayer> squad', 'int newPoints']                                     | {}         |
| FullGearCommand     | ['RunnerPlayer player']                                                            | {}         |
| AddTicketsCommand   | ['RunnerPlayer player', 'int tickets']                                             | {}         |
| ListCommand         | ['RunnerPlayer player']                                                            | {}         |
| ZombieCommand       | ['RunnerPlayer player']                                                            | {}         |
| void                | ['RunnerPlayer source', 'RunnerPlayer target']                                     | {}         |
| void                | ['RunnerPlayer caller']                                                            | {}         |
| ResetBuildCommand   | ['RunnerPlayer caller']                                                            | {}         |
| MapCommand          | ['RunnerPlayer caller']                                                            | {}         |
| PosCommand          | ['RunnerPlayer caller']                                                            | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| loadout             | ['RunnerPlayer player', 'ZombiePersistence loadout']                               | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| requestedPercentage | ['string name', 'float requestedPercentage', 'Action<RunnerPlayer> applyToPlayer'] | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| Reset               | ['']                                                                               | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |