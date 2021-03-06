# Monkeland

Changes to respect main branch:
 - Ported to Partiality
 - Rewrite of packet handling, removed specific methods in favor of general methods to allow more entities to be synced in the future.
 - In-Game pause menu with player list and exit button.
 - Player arrow tags.

## Approach
I tried an encapsulating approach to packets following the RW system.
Realized and Abstract fields are separated. If you call for example, `Spear.Read`, it will read all the fields up to PhysicalObject [PlayerCarryableItem->Weapon->Spear]. 
The order to fill all the fields of a Rock would be:
1) Call AbstractPhysicalObject.Read()
2) Call PhysicalObject.Read()
3) Call SpecificPhysicalObject.Read()

Steps 2 and 3 could be combined into one but left separate so could access fields in between.


## Some Packet Formats
### This a temporal set up of the packet format. The sizes are not accurate.
Physical Object Packet
| type  | field              | size (bytes) |
|-------|--------------------|:------------:|
| float | bounce             |       4      |
| bool  | canBeHitByWeapons  |       1      |
| byte  | numberOfChunks     |       1      |
| pkt   | BODYCHUNKPCKT      |              |
| byte  | numberOfChunkConns |       1      |
| pkt   | BODYCHUNKSCONNPKT  |              |

Abstract Creature Packet
| type       | field                             | size (bytes) |
|------------|-----------------------------------|:------------:|
| pkt        | Abstract Physical Object   Packet |              |
| byte       | Creature template type            |       1      |
| int        | remainInDenCounter                |       4      |
| worldCoord | spawnDen                          |      16      |

Abstract Physical Object
| type | field                          | size (bytes) |
|------|--------------------------------|:------------:|
| pkt  | Abstract World Entity   Packet |              |
| byte | AbsPhysType                    |       1      |
|      | (optional)                     |              |
| pkt  | AbstractSpear                  |              |

Abstract World Entity Packet
| type       | field         | size (bytes) |
|------------|---------------|:------------:|
| EntityID   | ID            |       8      |
| WorldCoord | pos           |      16      |
| bool       | inDen         |       1      |
| int        | timeSpentHere |       4      |

Physical Entity Packet
| type   | field                    | size (bytes) |
|--------|--------------------------|:------------:|
| byte   | packetType               |       1      |
| string | roomName                 |     ~26?     |
| int    | distinguisher            |       4      |
| byte   | AbsPhyObj type           |       1      |
| pkt    | Abstract Physical Object |              |
| pkt    | Physical Object Packet   |              |
| pkt    | Especific Obj Packet     |              |

Creature Entity Packet
| type   | field                    | size (bytes) |
|--------|--------------------------|:------------:|
| byte   | packetType               |       1      |
| string | roomName                 |     ~26?     |
| int    | distinguisher            |       4      |
| byte   | AbsPhyObj type           |       1      |
| pkt    | Abstract Physical Object |              |
| pkt    | Physical Object Packet   |              |
| pkt    | Especific Obj Packet     |              |
